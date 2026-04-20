<!-- PROJECT SHIELDS -->
[![NuGet][nuget-shield]](https://www.nuget.org/packages/Afip.Net)
[![Contributors][contributors-shield]](https://github.com/afipsdk/afip.net/graphs/contributors)
[![Closed issues][issues-shield]](https://github.com/afipsdk/afip.net/issues)
[![License][license-shield]](https://github.com/afipsdk/afip.net/blob/main/LICENSE)

<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/afipsdk/afip.net">
    <img src="https://github.com/afipsdk/afipsdk.github.io/blob/master/images/logo-colored.png" alt="Afip.Net" width="130" height="130">
  </a>

  <h3 align="center">Afip.Net</h3>

  <p align="center">
    Librería para conectarse a los Web Services de AFIP con .NET
    <br />
    <a href="https://docs.afipsdk.com"><strong>Explorar documentación »</strong></a>
    <br />
    <br />
    <a href="https://discord.gg/A6TuHEyAZm"><strong>Comunidad Afip SDK</strong></a>
    <br />
    <br />
    <a href="https://github.com/afipsdk/afip.net/issues">Reportar un bug</a>
  </p>
</p>

<!-- DOCS -->
## Documentación
[Explorar documentación](https://docs.afipsdk.com)

<!-- COMUNITY -->
## Comunidad
[Comunidad Afip SDK](https://discord.gg/A6TuHEyAZm)

<!-- ABOUT THE PROJECT -->
## Acerca del proyecto
Con más de 100k descargas, desde el 2017, Afip SDK es la plataforma preferida entre los desarrolladores para conectarse a los web services de ARCA.
Esta librería permite integrar fácilmente la facturación electrónica y otros servicios de ARCA en aplicaciones .NET.

## Versiones soportadas

| Plataforma | Versión mínima |
|---|---|
| .NET Framework | 4.6.1 |
| .NET Core | 2.0 |
| .NET | 5.0+ |

## Instalación

```bash
dotnet add package Afip.Net
```

## Uso

```csharp
using Afip.Net;
using AfipClient = Afip.Net.Afip;

var afip = new AfipClient(new AfipOptions
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

<!-- CONTACT -->
### Contacto
Soporte de Afip SDK - ayuda@afipsdk.com

Link del proyecto: [https://github.com/afipsdk/afip.net](https://github.com/afipsdk/afip.net)

_Este software y sus desarrolladores no tienen ninguna relación con la AFIP._

<!-- MARKDOWN LINKS & IMAGES -->
[nuget-shield]: https://img.shields.io/nuget/dt/Afip.Net.svg
[contributors-shield]: https://img.shields.io/github/contributors/afipsdk/afip.net.svg?color=orange
[issues-shield]: https://img.shields.io/github/issues-closed-raw/afipsdk/afip.net.svg?color=blueviolet
[license-shield]: https://img.shields.io/github/license/afipsdk/afip.net.svg?color=blue
