@echo off
echo Procurando pastas bin e obj...
for /f "delims=" %%i in ('dir /s /b /ad bin 2^>nul') do (
    echo Removendo %%i
    rd /s /q "%%i"
)
for /f "delims=" %%i in ('dir /s /b /ad obj 2^>nul') do (
    echo Removendo %%i
    rd /s /q "%%i"
)
echo Limpeza concluída!
pause