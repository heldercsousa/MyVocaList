// CommunityToolkit — used by every ViewModel
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
// DevExpress — used by every Page and ViewModel
global using DevExpress.Maui;
global using DevExpress.Maui.CollectionView;
global using DevExpress.Maui.Controls;
global using DevExpress.Maui.Core;
global using Microsoft.Extensions.Logging;
// Contracts — every feature ViewModel + Page will need DTOs and pagination constants
global using MyVocaList.Contracts;
global using MyVocaList.Contracts.DTOs.List;
global using MyVocaList.Contracts.Models;
// App navigation and models
global using MyVocaList.Navigation;
// Services layer
global using MyVocaList.Domain.ServicesInterfaces;
global using MyVocaList.Services;
global using MyVocaList.UI.Models;
// Pages — registered in MauiProgram and NavigationConfig
global using MyVocaList.UI.Pages.Artists;
global using MyVocaList.UI.Pages.BackupRestore;
global using MyVocaList.UI.Pages.Events;
global using MyVocaList.UI.Pages.People;
global using MyVocaList.UI.Pages.Preferences;
global using MyVocaList.UI.Pages.Queue;
global using MyVocaList.UI.Pages.Venues;
global using MyVocaList.UI.Components;
global using MyVocaList.UI.ViewModels;
