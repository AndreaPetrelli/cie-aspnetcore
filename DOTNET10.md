# Fork .NET 10

Il branch `feat/dotnet10` adegua il progetto della libreria CIE a `net10.0`.
Richiede SDK .NET 10 e runtime .NET/ASP.NET Core 10.0.13 o una patch 10.0 successiva.

Le API HTTP, Identity e crittografia XML sono fornite dal framework
`Microsoft.AspNetCore.App`; i precedenti pacchetti Microsoft 6/7/8/9 non sono
più referenziati. Per le patch di sicurezza del framework aggiornare il runtime
installato o l'immagine del servizio che ospita la libreria.

Il progetto è compilabile da solo, senza file di configurazione di MPWeb2:

```bash
dotnet restore CIE.AspNetCore.Authentication/CIE.AspNetCore.Authentication/CIE.AspNetCore.Authentication.csproj --tl:off -m:1 /nr:false /p:UseSharedCompilation=false
dotnet build CIE.AspNetCore.Authentication/CIE.AspNetCore.Authentication/CIE.AspNetCore.Authentication.csproj --no-restore --tl:off -m:1 /nr:false /p:UseSharedCompilation=false
```

Questa modifica riguarda la libreria, non aggiorna i progetti di esempio upstream.
Nomi e versioni degli assembly, risorse, generazione e firma SAML sono mantenuti.
Il superamento dei test dell'applicazione non sostituisce il collaudo federato.
