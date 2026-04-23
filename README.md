# Afip.Net

[![NuGet](https://img.shields.io/nuget/dt/Afip.Net.svg)](https://www.nuget.org/packages/Afip.Net)
[![Contributors](https://img.shields.io/github/contributors/afipsdk/afip.net.svg?color=orange)](https://github.com/afipsdk/afip.net/graphs/contributors)
[![License](https://img.shields.io/github/license/afipsdk/afip.net.svg?color=blue)](https://github.com/afipsdk/afip.net/blob/main/LICENSE)

Librería para conectarse a los Web Services de AFIP con .NET

[Explorar documentación](https://docs.afipsdk.com) · [Comunidad Afip SDK](https://discord.gg/A6TuHEyAZm) · [Reportar un bug](https://github.com/afipsdk/afip.net/issues)

## Acerca del proyecto

Con más de 100k descargas, desde el 2017, Afip SDK es la plataforma preferida entre los desarrolladores para conectarse a los web services de ARCA.
Esta librería permite integrar fácilmente la facturación electrónica y otros servicios de ARCA en aplicaciones .NET.

## Versiones soportadas

| Plataforma | Versión mínima
|---|---
| .NET Framework | 4.6.1
| .NET Core | 2.0
| .NET | 5.0+

## Instalación

```bash
dotnet add package Afip.Net
```

## Uso

```csharp
using AfipSDK.Afip.Net;

var afip = new Afip(new AfipOptions
{
    CUIT = "TU_CUIT",
    Production = false,
    AccessToken = "TU_ACCESS_TOKEN",
});
```

### Facturación electrónica

```csharp
// Obtener el último número de comprobante
var puntoDeVenta = 1;
var tipoDeFactura = 6; // 6 = Factura B

var lastVoucher = await afip.ElectronicBilling.GetLastVoucherAsync(puntoDeVenta, tipoDeFactura);
var numeroDeFactura = lastVoucher + 1;

// Crear comprobante
var data = new Dictionary<string, object?>
{
    ["CantReg"]  = 1,
    ["PtoVta"]   = puntoDeVenta,
    ["CbteTipo"] = tipoDeFactura,
    ["Concepto"] = 1,           // 1 = Productos
    ["DocTipo"]  = 99,          // 99 = Consumidor Final
    ["DocNro"]   = 0,
    ["CbteDesde"] = numeroDeFactura,
    ["CbteHasta"] = numeroDeFactura,
    ["CbteFch"]  = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd")),
    ["ImpTotal"] = 121m,
    ["ImpTotConc"] = 0,
    ["ImpNeto"]  = 100m,
    ["ImpOpEx"]  = 0,
    ["ImpIVA"]   = 21m,
    ["ImpTrib"]  = 0,
    ["MonId"]    = "PES",
    ["MonCotiz"] = 1,
    ["CondicionIVAReceptorId"] = 5, // 5 = Consumidor Final
    ["Iva"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Id"]      = 5, // 5 = 21%
            ["BaseImp"] = 100m,
            ["Importe"] = 21m
        }
    }
};

var response = await afip.ElectronicBilling.CreateVoucherAsync(data);
Console.WriteLine($"CAE: {response["CAE"]}");
Console.WriteLine($"Vencimiento: {response["CAEFchVto"]}");
```

## Documentación

[Explorar documentación](https://docs.afipsdk.com)

## Comunidad

[Comunidad Afip SDK](https://discord.gg/A6TuHEyAZm)

## Contacto

Soporte de Afip SDK - ayuda@afipsdk.com

[https://github.com/afipsdk/afip.net](https://github.com/afipsdk/afip.net)

_Este software y sus desarrolladores no tienen ninguna relación con la AFIP._
