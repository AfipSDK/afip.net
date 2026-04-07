# Example: Create Voucher

Esta carpeta contiene un ejemplo de uso básico para crear un comprobante utilizando la librería `Afip.Net`.

## Uso

1. Reemplaza `YOUR_ACCESS_TOKEN` en `CreateVoucher.cs` por tu token de AfipSDK.
2. Ajusta los valores de `Cuit` y los campos del comprobante según tus datos.
3. Compila y ejecuta el archivo desde un proyecto de ejemplo o con `dotnet run` si lo agregas a un proyecto.

```bash
cd examples
# Crear un proyecto de ejemplo si hace falta
# dotnet new console -n AfipExample
# Copia CreateVoucher.cs en el proyecto y luego:
# dotnet run
```

## Contenido

- `CreateVoucher.cs`: ejemplo inspirado en `afip.js/examples/createVoucher.js`.
