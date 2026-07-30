# Índice de Cambios - Octubre 2024 a Enero 2025

## Última Actualización
**2025-01-15** - Implementación de autenticación y persistencia en Supabase

---

## 📚 Changelog del Proyecto

### 2025-01: Autenticación y Persistencia en Supabase
**Archivo:** `2025_01-autenticacion-supabase.md`

Implementación completa de:
- ✅ Sistema de autenticación con Supabase (SignIn, SignUp, SignOut)
- ✅ CRUD de sesiones de observación
- ✅ Row Level Security (RLS) en base de datos
- ✅ ViewModel MVVM con propiedades observables
- ✅ Stubs para SSR (no-op)
- ✅ Localización a 6 idiomas (18 claves nuevas)
- ✅ Configuración segura (appsettings.Local.json)

**Archivos creados:** 9
**Archivos modificados:** 15
**Estado:** ✅ Compilable y listo para usar

---

### 2026-06: Refactoring de Arquitectura Completa
**Archivo:** `2026_06-refactoring-arquitectura-completa.md`

Reorganización del proyecto en capas compartidas:
- Centralización de lógica en ObservApp.Shared (RCL)
- Patrón de interfaces compartidas
- Implementaciones específicas por plataforma

---

### 2026-05: Calculadora de Sol y Luna
**Archivo:** `2026_05-calculadora-sol-luna.md`

Nuevas calculadoras astronómicas:
- Cálculos de posición solar y lunar
- Integración con AstronomyEngine

---

### 2026-04: Arquitectura Base
**Archivo:** `2026_04-arquitectura-base.md`

Estructura inicial:
- Proyecto MAUI + Blazor compartido
- Estructura de carpetas
- Configuración de recursos

---

## 🔍 Cómo Navegar

### Por Funcionalidad

#### Autenticación y Bases de Datos
- Ver: `2025_01-autenticacion-supabase.md`
- Archivo técnico: `Documentacion/Supabase_SQL_Schema.sql`
- Localización: `Documentacion/Localization_Summary.md`

#### Arquitectura General
- Ver: `2026_06-refactoring-arquitectura-completa.md`
- Estructura de proyectos: `2026_04-arquitectura-base.md`

#### Calculadoras Astronómicas
- Ver: `2026_05-calculadora-sol-luna.md`

### Por Proyecto

#### 📱 ObservApp (MAUI)
- Plataformas: Android, Windows
- Características: Autenticación real, CRUD real
- Ver: `2025_01-autenticacion-supabase.md`

#### 🌐 ObservApp.Web.Client (Blazor WASM)
- Plataforma: Web (navegador)
- Características: Autenticación real, CRUD real
- Ver: `2025_01-autenticacion-supabase.md`

#### 🖥️ ObservApp.Web (ASP.NET Core SSR)
- Plataforma: Servidor
- Características: Renderizado en servidor, stubs de auth
- Ver: `2025_01-autenticacion-supabase.md`

#### 📦 ObservApp.Shared (Razor Class Library)
- Contenido: Interfaces, ViewModels, Modelos
- Características: Agnóstico a plataforma
- Ver: Todos los changelog

---

## 📋 Estado Actual (2025-01)

| Componente | Estado | Documentación |
|------------|--------|-----------------|
| Autenticación | ✅ Implementado | `2025_01-autenticacion-supabase.md` |
| CRUD Observaciones | ✅ Implementado | `2025_01-autenticacion-supabase.md` |
| Base de Datos | ✅ Schema completo | `Supabase_SQL_Schema.sql` |
| Localización | ✅ 6 idiomas, 18 claves | `Localization_Summary.md` |
| MVVM ViewModel | ✅ Completo | `2025_01-autenticacion-supabase.md` |
| AppState | ✅ Actualizado | `2025_01-autenticacion-supabase.md` |
| Compilación | ✅ Exitosa | - |

---

## 🚀 Integración Recomendada

Si eres nuevo en el proyecto, sigue este orden:

1. **Lee arquitectura general**
   - `2026_06-refactoring-arquitectura-completa.md`
   - `2026_04-arquitectura-base.md`

2. **Entiende la autenticación**
   - `2025_01-autenticacion-supabase.md` (Secciones 1-3)

3. **Configura credenciales**
   - `2025_01-autenticacion-supabase.md` (Sección "Configuración")
   - `2025_01-autenticacion-supabase.md` (Sección "Guía de Instalación")

4. **Revisa las claves i18n**
   - `Localization_Summary.md`

5. **Implementa UI según tu plataforma**
   - MAUI: Crea LoginPage.xaml
   - Blazor: Crea componente Login.razor

6. **Prueba localmente**
   - Con appsettings.Local.json configurado

---

## 📝 Cómo Documentar Nuevos Cambios

Al agregar nuevas funcionalidades:

1. Crea archivo: `Documentacion/changelog/YYYY_MM-descripcion.md`
2. Usa esta estructura:
   ```
   # YYYY-MM - Descripción del Cambio

   **Fecha:** 
   **Versión:**
   **Alcance:**
   **Estado:**

   ## Tabla de Contenidos
   ## Objetivo
   ## Cambios Implementados
   ## Archivos Creados/Modificados
   ## Próximos Pasos
   ```

3. Actualiza este INDEX.md

---

## 🔗 Enlaces Rápidos

- **Supabase Docs**: https://supabase.com/docs
- **MAUI Docs**: https://learn.microsoft.com/en-us/dotnet/maui/
- **Blazor Docs**: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- **Community Toolkit**: https://github.com/CommunityToolkit/dotnet
- **GitHub Proyecto**: https://github.com/ebenito/ObservApp

---

## 📊 Estadísticas del Proyecto (2025-01)

| Métrica | Valor |
|---------|-------|
| Archivos de changelog | 5 |
| Documentación técnica | 3+ |
| Proyectos principales | 4 |
| Idiomas soportados | 6 |
| Claves i18n agregadas | 18 |
| Servicios de autenticación | 3 |
| Líneas de código nuevo | ~1000+ |

---

**Última actualización:** 2025-01-15  
**Próxima revisión sugerida:** Después de implementar UI de login
