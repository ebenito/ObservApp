# Copilot Instructions

## Directrices del proyecto
- En ObservApp.Shared se deben seguir estos patrones: solo interfaces inyectadas, textos visibles con IStringLocalizer y prefijo Eph_ en Efemérides, ChangeEventArgs totalmente calificado, no usar @bind con @oninput, SVG con InvariantCulture y MarkupString pregenerado, y cálculo astronómico con Observer usando altitud en km.
- Si se realiza un cambio en la UI de ObservApp, no debe quedar ningún texto accidental de metadatos o explicaciones visible en la interfaz final.