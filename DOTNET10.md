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


## Compatibilità API .NET 10

- Gli handler usano il costruttore moderno di `RemoteAuthenticationHandler`,
  con il `TimeProvider` delle opzioni dello schema, senza `ISystemClock`.
- La generazione degli URL usa `IHttpContextAccessor`, i dati di routing e il
  descrittore dell'endpoint corrente. Una registrazione applicativa di
  `IUrlHelper` conserva la precedenza.
- I certificati PFX vengono letti con `X509CertificateLoader.LoadPkcs12*`;
  i certificati pubblici con `LoadCertificate*`. Password richieste e flag
  `MachineKeySet | PersistKeySet | Exportable` restano invariati. I certificati
  pubblici usati nella verifica delle firme vengono rilasciati dopo la verifica.
- Nei file che già usano annotazioni nullable viene abilitato il relativo
  contesto, senza attivare globalmente nuovi controlli nullable.
- Rimossi i confronti sempre falsi di `DateTime` con `null`; restano i controlli
  esistenti sui valori minimi, gli intervalli temporali e la scadenza.

Generazione XML, algoritmi di firma e verifica, dati richiesti e endpoint SAML
restano quelli della libreria.


## Download e lettura dei messaggi

I download di metadata/registro usano il client nominato (`cie` o `spid`) di
`IHttpClientFactory`, con verifica TLS standard, solo URL HTTPS senza credenziali,
timeout massimo di 100 secondi per CIE e cancellazione della richiesta. Anche il corpo
letto in streaming è limitato a 4 MiB, senza fidarsi del solo Content-Length.

La cache è isolata per istanza di opzioni e URL: CIE scade dopo un'ora quando
`CacheIdpMetadata` è attivo; SPID mantiene `IdpMetadataCacheDurationInMinutes`.
Un errore nel rinnovo viene propagato, senza riutilizzare una cache
scaduta o memorizzare la risposta fallita. SPID restituisce una nuova lista e non
aggiunge i provider remoti alla collezione configurata. È disponibile anche
l'overload cancellabile di `GetIdentityProviders`; quello esistente rimane.

Le risposte di login POST/Redirect vengono limitate a 1 MiB decodificato, anche
dopo DEFLATE, prima dei log e della deserializzazione. DTD e risorse XML esterne
sono proibiti per questi ingressi e per i metadata XML CIE; l'XML valido viene
conservato esattamente. I limiti sono attualmente costanti della libreria.

Questa modifica non corregge la fiducia nelle firme del logout e non cambia
algoritmi, firma dei metadata, politica delle chiavi private o tolleranze temporali.
