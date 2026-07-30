# Localización de Autenticación y Sesiones de Observación

## Resumen
Se han agregado 18 nuevas claves de localización a todos los idiomas soportados por ObservApp para funcionalidades de autenticación y gestión de sesiones de observación.

## Idiomas Soportados
- ✅ Español (es-ES)
- ✅ Inglés (en-US)
- ✅ Alemán (de-DE)
- ✅ Francés (fr-FR)
- ✅ Italiano (it-IT)
- ✅ Árabe (ar-AR)

## Claves de Autenticación (Auth_*)

| Clave | Español | English | Deutsch | Français | Italiano | العربية |
|-------|---------|---------|---------|----------|----------|---------|
| Auth_SignIn | Iniciar sesión | Sign In | Anmelden | Se connecter | Accedi | تسجيل الدخول |
| Auth_SignUp | Registrarse | Sign Up | Registrieren | S'inscrire | Registrati | إنشاء حساب |
| Auth_SignOut | Cerrar sesión | Sign Out | Abmelden | Se déconnecter | Esci | تسجيل الخروج |
| Auth_Email | Correo electrónico | Email | E-Mail | Adresse e-mail | Email | البريد الإلكتروني |
| Auth_Password | Contraseña | Password | Passwort | Mot de passe | Password | كلمة المرور |
| Auth_DisplayName | Nombre mostrado | Display Name | Anzeigename | Nom d'affichage | Nome visualizzato | اسم العرض |
| Auth_SigningIn | Iniciando sesión... | Signing in... | Wird angemeldet... | Connexion en cours... | Accesso in corso... | جاري تسجيل الدخول... |
| Auth_Error | Error de autenticación | Authentication error | Authentifizierungsfehler | Erreur d'authentification | Errore di autenticazione | خطأ في المصادقة |

## Claves de Sesiones de Observación (Obs_*)

| Clave | Español | English | Deutsch | Français | Italiano | العربية |
|-------|---------|---------|---------|----------|----------|---------|
| Obs_Sessions | Sesiones de observación | Observation Sessions | Beobachtungssitzungen | Sessions d'observation | Sessioni di osservazione | جلسات الرصد |
| Obs_NewSession | Nueva sesión | New Session | Neue Sitzung | Nouvelle session | Nuova sessione | جلسة جديدة |
| Obs_Title | Título | Title | Titel | Titre | Titolo | العنوان |
| Obs_Date | Fecha | Date | Datum | Date | Data | التاريخ |
| Obs_Notes | Notas | Notes | Notizen | Remarques | Note | ملاحظات |
| Obs_Location | Ubicación | Location | Standort | Emplacement | Posizione | الموقع |
| Obs_Seeing | Seeing | Seeing | Seeing | Seeing | Seeing | الرؤية |
| Obs_Transparency | Transparencia | Transparency | Transparenz | Transparence | Trasparenza | الشفافية |
| Obs_Save | Guardar | Save | Speichern | Enregistrer | Salva | حفظ |
| Obs_Delete | Eliminar | Delete | Löschen | Supprimer | Elimina | حذف |

## Archivos Modificados

1. **ObservApp.Shared/Resources/Strings/App.es.resx** - Español ✅
2. **ObservApp.Shared/Resources/Strings/App.en.resx** - English ✅
3. **ObservApp.Shared/Resources/Strings/App.de.resx** - Deutsch ✅
4. **ObservApp.Shared/Resources/Strings/App.fr.resx** - Français ✅
5. **ObservApp.Shared/Resources/Strings/App.it.resx** - Italiano ✅
6. **ObservApp.Shared/Resources/Strings/App.ar.resx** - العربية ✅

## Uso en Código

Las claves se pueden usar mediante `AppStrings`:

```csharp
// Ejemplo: Obtener texto localizado
string signInText = AppStrings.Auth_SignIn;
string sessionTitle = AppStrings.Obs_Sessions;
```

O mediante inyección de dependencia de `ILocalizationService`:

```csharp
public class AuthViewModel(ILocalizationService localization)
{
	private string GetLocalizedText(string key) => 
		localization.GetString(key);
}
```

## Notas de Traducción

- **Seeing** y **Transparency**: Se mantiene sin traducción ya que son términos técnicos astronomía comúnmente usados en todos los idiomas.
- **Árabe**: Las direcciones de texto (RTL) se manejan automáticamente en MAUI/Blazor.
- Todas las traducciones se han realizado considerando el contexto de una aplicación astronómica.

## Verificación

✅ Compilación exitosa en todos los proyectos
✅ Todos los idiomas tienen exactamente 18 claves nuevas
✅ Formato XML correctamente formado en todos los .resx
