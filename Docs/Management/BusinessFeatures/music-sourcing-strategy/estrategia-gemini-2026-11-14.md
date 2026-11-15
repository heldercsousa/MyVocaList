# Arquitetura e Estratégias de Desenvolvimento — Aplicativo de Karaokê (.NET MAUI)

> Origem: chat com o Gemini web em 2026-11-14. Conteúdo a avaliar — nada aqui foi adotado.

Este documento consolida os pontos arquiteturais a avaliar, integrações de APIs, regras de direitos autorais e estratégias recomendadas para o desenvolvimento de um aplicativo multiplataforma (**Windows e Android**) voltado tanto para **Karaokê tradicional** quanto para **Bandokê (banda ao vivo)**.

---

## 1. Visão Geral do Ecossistema de Dados

O sistema combina três fontes principais para formar um catálogo completo sem depender exclusivamente de buscas manuais:

| Fonte de Dados | Papel no Sistema | Armazenamento Local | Custo / Licenciamento |
| :--- | :--- | :--- | :--- |
| **Deezer API** | Metadados oficiais (Título, Artista, Álbum, Capa quadrada, Duração). | **Permanente** (SQLite local) | Gratuito (API aberta para buscas) |
| **LRCLIB** | Letras simples (`plainLyrics`) e sincronizadas (`syncedLyrics` / Enhanced LRC). | **Permanente** (SQLite local) | Gratuito / Open Source (sem API Key) |
| **YouTube Data API v3** | Busca integrada de vídeos de karaokê (`videoId`). | **Apenas o `videoId`** (11 caracteres) | Gratuito (10.000 unidades/dia) |
| **YouTube IFrame API** | Reprodução oficial dentro do app (`WebView`) sem violação de direitos. | N/A (Streaming oficial) | Gratuito (sujeito às regras de UI) |

---

## 2. Estratégia Recomendada para Integração com YouTube (Compliance e Cota)

Para eliminar a necessidade de o usuário sair do app para copiar/colar URLs, mantendo total conformidade com os **Termos de Serviço do YouTube** e economizando a cota diária da API:

### A. Armazenamento Minimalista (Apenas `videoId`)
* **O que salvar no banco local:** Armazene **apenas o `videoId`** (ex: `dQw4w9WgXcQ`) vinculado ao ID da faixa do Deezer.
* **Por que isso é recomendado:**
  * A regra do YouTube que exige atualização de cache a cada 30 dias aplica-se a metadados (títulos, descrições, URLs de imagens), **não ao `videoId` puro**.
  * Reduz o tamanho do banco de dados local e elimina rotinas complexas de expiração de cache.
  * **Retrocompatibilidade:** Links antigos colados manualmente pelos usuários podem ser convertidos facilmente via Regex para extrair apenas os 11 caracteres do `videoId`.

### B. Obtenção de Thumbnails e Metadados em Tempo de Execução
1. **Thumbnails (Custo Zero de Cota):**
   Monte a URL diretamente na UI do .NET MAUI usando o `videoId` salvo, sem consultar a API:
   * Lista / Cards (320x180): `https://ytimg.com{videoId}/mqdefault.jpg`
   * Alta qualidade universal (480x360): `https://ytimg.com{videoId}/hqdefault.jpg`
   * HD (1280x720 — pode retornar 404 em vídeos antigos): `https://ytimg.com{videoId}/maxresdefault.jpg`
2. **Título e Duração em Lote (`videos.list` — Custo ~Zero):**
   * Ao carregar uma página de resultados ou fila de reprodução, consulte `GET /youtube/v3/videos?part=snippet,contentDetails,status&id=ID1,ID2,...,ID50`.
   * **Custo:** Apenas **1 unidade de cota** para buscar até **50 vídeos de uma vez** (permite consultar até 500.000 vídeos/dia na cota gratuita).
   * **Benefício extra:** Verifica em tempo real se o vídeo ainda está disponível e incorporável (`status.embeddable == true`).
3. **Consulta Unitária sem API Key (`oEmbed`):**
   * Para detalhes rápidos de um único vídeo ao abrir o player: `https://youtube.com{videoId}&format=json` (não retorna duração, apenas título e autor).

### C. Regras Obrigatórias de Interface (YouTube IFrame Player API)
Para evitar bloqueio da chave de API pelo Google:
* **Zero Overlays:** Nunca sobreponha letras, botões, placares ou textos por cima da área do vídeo. A letra sincronizada do LRCLIB deve ficar em um painel separado (abaixo ou ao lado do player).
* **Dimensões Mínimas:** A `WebView` do player deve ter no mínimo **200x200 px** (recomendado: mínimo 480x270 px em proporção 16:9). Nunca oculte o vídeo para tocar apenas o áudio.
* **Cabeçalho `Referer`:** Configure a `WebView` no Android e Windows para enviar o identificador do pacote do seu aplicativo no cabeçalho HTTP `Referer`.
* **Controles Originais:** Não bloqueie anúncios nem oculte a marca d'água/controles nativos do YouTube.

---

## 3. Pré-Alimentação Estratégica com Sistema de Recomendação por IA

Como o endpoint `search.list` custa **100 unidades** por busca (limite de 100 buscas/dia na cota gratuita), a busca ao vivo deve ser usada apenas como último recurso.

### Fluxo de Curadoria Antecipada (Background Worker)
1. **Coleta de Perfil do Evento (Input do Host):**
   * Gêneros musicais predominantes (ex: Sertanejo, Rock Nacional, Pagode, Pop Internacional).
   * País de origem e idioma principal.
   * Estatísticas locais de músicas/artistas mais executados em eventos anteriores.
2. **Geração de Lista via IA (LLM):**
   * Um serviço em segundo plano (preferencialmente no desktop Windows) solicita à IA uma lista priorizada de músicas clássicas e populares de karaokê aderentes ao perfil do evento.
3. **Indexação em Lote (Aproveitamento de Cota Ociosa):**
   * Nos dias que antecedem o evento, o sistema consome a cota diária renovável (10.000 unidades/dia) para buscar as faixas na API do Deezer, baixar a letra no LRCLIB e localizar o melhor vídeo incorporável (`videoEmbeddable=true`) no YouTube, salvando os vínculos no banco local.
4. **Resolução Instantânea no Evento:**
   * Durante a festa, 80% a 90% das pesquisas já estarão indexadas localmente, carregando instantaneamente sem consumir cota.

---

## 4. Modo Bandokê (Banda Ao Vivo) vs. Modo Áudio Playback

Bandas ao vivo variam o andamento (BPM), alteram o tom (*pitch*) para adaptar ao cantor e estendem introduções ou solos. Além disso, cantores amadores frequentemente não conhecem o ritmo nem a letra da música. Portanto, **timestamps fixos de estúdio (`.lrc`) e reconhecimento de voz do cantor não devem guiar o tempo no ao vivo.**

### Estratégia Recomendada para Bandokê: Teleprompter Reativo Guiado pela Banda

1. **Quebra Estrutural Automática:**
   * Utilize o arquivo `.lrc` do LRCLIB (ou `plainLyrics`) apenas para dividir a música em blocos lógicos (`[INTRO]`, `[VERSO 1]`, `[REFRÃO]`, `[SOLO]`) e calcular a velocidade relativa de leitura de cada estrofe.
2. **Controle de Disparo por Pedal Bluetooth / Músico (Re-sincronia por Bloco):**
   * O baterista ou tecladista utiliza um pedal Bluetooth (simulando teclas `PageDown` / `Seta para Baixo`) ou toque na tela para disparar o início de cada estrofe.
   * Ao disparar o bloco, o app anima o preenchimento da letra usando o tempo relativo calculado, mas **aguarda o próximo toque do músico para iniciar a estrofe seguinte**. Isso absorve qualquer variação de BPM ou improviso da banda.
3. **Regente Visual para o Cantor Perdido:**
   * **Count-in Visual:** Antes de cada entrada vocal, exibe uma contagem pulsante (`4 - 3 - 2 - 1`) para indicar o momento exato de começar a cantar.
   * **Metrônomo Visual Compartilhado:** Opcionalmente, exibe um pulso luminoso na tela baseado no BPM ajustável da faixa para manter banda e tela alinhados.

---

## 5. Alternativa Legal para Áudio Local sem YouTube ("BYO Audio")

Para oferecer karaokê de qualquer música sem depender do catálogo do YouTube e **sem infringir direitos autorais**:

* **O que NÃO fazer:** Nunca hospede, venda ou distribua arquivos `.mp3`/`.wav` com a voz removida por IA em servidores próprios. Isso configura distribuição não autorizada de obra derivada (violação de direitos de fonograma).
* **Modelo Seguro ("Traga Seu Próprio Áudio" — BYO Audio):**
  * Posicione o aplicativo como um **software utilitário/player local**, onde o próprio usuário ou estabelecimento fornece os arquivos de áudio que já possui legalmente.
  * **Pipeline de Pré-Processamento (Windows Desktop):**
    1. **Separação de Faixas (Source Separation):** Utilize modelos locais open-source como **Demucs (Meta AI)** ou **MDX-Net (UVR)** para separar o áudio em `instrumental.wav` e `vocals.wav`.
    2. **Obtenção de Letra (Cascata de Fallback):**
       * *Nível 1:* Busca `syncedLyrics` no **LRCLIB**.
       * *Nível 2:* Se houver apenas `plainLyrics` (ou texto de APIs de letras), segue para o alinhamento.
       * *Nível 3:* Se a música não existir em nenhum banco, transcreve o `vocals.wav` localmente com **Whisper (`large-v3`)**.
    3. **Alinhamento Forçado (Forced Alignment):** Utilize **WhisperX** ou **Stable-TS** cruzando o texto da letra com a trilha `vocals.wav` isolada para gerar um arquivo **Enhanced LRC** sincronizado palavra por palavra.
    4. **Execução Offline:** O pacote final (`instrumental` + `.lrc` + metadados Deezer) fica salvo localmente para reprodução instantânea no .NET MAUI.

---

## 6. Matriz Técnica a Avaliar (.NET MAUI)

| Cenário de Uso | Fonte de Áudio | Sincronia da Letra | Risco Jurídico | Custo Operacional |
| :--- | :--- | :--- | :--- | :--- |
| **Karaokê Online (Catálogo Geral)** | YouTube IFrame Player API | LRCLIB em painel separado (sem overlay no vídeo) | **Baixo** (regras do YouTube) | **Zero** (com cache de `videoId` e IA preditiva) |
| **Karaokê Offline (Qualquer Música)** | Arquivo local do usuário + Demucs | Enhanced LRC gerado localmente via WhisperX | **Muito Baixo** (uso privado/local) | **Zero** (processamento na máquina do usuário) |
| **Bandokê (Banda Ao Vivo)** | Nenhuma (Banda toca ao vivo) | Blocos do LRCLIB + Pedal Bluetooth + Count-in visual | **Zero** | **Zero** |
