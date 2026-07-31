# Copilot Instructions

## Directrices del proyecto
- En ObservApp.Shared se deben seguir estos patrones: solo interfaces inyectadas, textos visibles con IStringLocalizer<App>, prefijo Eph_ en Efemérides, ChangeEventArgs totalmente calificado, no usar @bind con @oninput, SVG con InvariantCulture y MarkupString pregenerado, y cálculo astronómico con Observer usando altitud en km.
- Los componentes Razor deben usar solo interfaces inyectadas, textos con IStringLocalizer<App>, nunca usar <form>, proteger páginas privadas con AuthGuard y usar estado local para paneles de detalle cuando se solicite.
- Si se realiza un cambio en la UI de ObservApp, no debe quedar ningún texto accidental de metadatos o explicaciones visible en la interfaz final.
- Antes de implementar páginas dependientes, verificar la existencia real de modelos y ViewModels en el workspace. La arquitectura anticipada no presente aún en el repo puede ser descrita en CLAUDE_CONTEXT.md.