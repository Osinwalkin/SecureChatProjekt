Secure Chat Mini Projekt

Christian Bech Fruchard

Project Beskrivelse
Der er opsummering og screenshots i bunden.

Jeg har brugt Blazor med lidt Javascript til at få nøglegenerering til at fungere.

Målet med dette mini projekt er at lave en to persons chat applikation der har fokus på confidentiality og integrity fra CIA triaden. Der gøres brug af kryptografiske principper heri.

Applikationen skal lave en secure session hvor to brugere (browser tabs) kan sende beskeder til hinanden. Der gøres brug af assymetrisk kryptografi når der hentes/deles keys og symmetrisk kryptografi til message encryption og integrity.

Dette er Proof-of-Concept og kan være lidt forvirrende at få til at virke, jeg prøvede at få SignalR til at virke i starten, men stødte på problemer med state opdatering i blazor.
Key exchange processen ligenu er manuel, hvilket ikke er helt optimalt men selve besked interaktionen skulle gerne virke.

Features
*   Secure session establishment imellem to brugere via RSA key exchange.
*   End-to-End Encryption (E2EE) af chat beskeder ved brug af AES-GCM.
*   Integrity beskyttelse ved brug af AES-GCM.
*   Lidt backend API til brug af message relay/mailbox.

Teknologi
*   .NET 8
*   Blazor WebAssembly (ASP.NET Core Hosted)
*   C#
*   JavaScript (til Web Crypto API via JS Interop)
*   HTML / CSS

Steps til at køre
1.  Klon repostiory
2.  Kør 'SecureChatProjekt' (ikke .Client)
3.  Åben en browser og brug localhost linket i consollen.
4.  Brug af chatten
    *   Åben to browser vinduer (Man kan bruge inkognito browser tabs hvis der kommer problemer fra extensions, jeg havde problemer med adblock og var nød til at bruge inkognito)
    *   Bruger 1 (Joe)
        *   Tryk på secure chat fanen
        *   Lave et user ID (Joe f.eks. men det er valgfrit)
        *   Skriv bruger 2's user ID ind i recipient User boksen (Ben)
        *   Kopier teksten fra "My Public Key (JWK)" text boksen.
    *   Bruger 2 (Ben)
        *   Tryk på secure chat fanen
        *   Lave et user ID (Ben f.eks. men det er valgfrit)
        *   Skriv bruger 1's user ID ind i Recipient User boksen (Joe)
        *   Kopier teksten fra "My Public Key (JWK)" text boksen.
    *   Bruger 1: copypaste bruger 2's public key ind i "Their Public Key" og klik "Set Their Key".
    *   Bruger 2: Paste Bruger 2's public key ind i "Their Public Key" og klik "Set Their Key".
    *   Bruger 1: klik på "Initiate Session" knappen. En besked der siger den sender "session key" dukker op i chat feltet
    *   Bruger 2: Applikationen skulle gerne fetche og decrypte nøglen automatisk og "Shared Secret Status" skulle gerne opdatere til "Established (Receiver)".
    *   Chat!: Nu skulle de to brugere gerne kunne skrive med hinanden

Kryptografi
Formålet med projektet er at sikre sig confidentiality i chatten (Hindrer andre i at læse beskeder der sendes) og integrity (sikrer sig at beskederne ikke kan ændres under afsending)


Etablering af sessionsnøgle (Confidentiality)

Jeg anvender en krypteringsmetode der hvor der deles en AES-nøgle mellem de to brguere uden at afsløre selve nøglen (kun til de to brugere)

Generering af RSA nøglepar: Hvert client genere et 2048-bit RSA-OAEP nøglepar ved hjælp af window.crypto.subtle.generateKey i JS interop. Nøglerne gemmes og udveksles i JSON Web Key (JWK) formatet.

Udveksling af nøgler: Hver bruger kopierer/indsætter manuelt deres offentlige JWK nøgler ind i den anden brugers applikation.

Generering & Kryptering af AES-nøgle: Én bruger (initiatoren) genererer en kryptografisk sikker 256-bit (32-byte) AES-nøgle ved hjælp af C#'s RandomNumberGenerator.GetBytes(32). Denne AES-nøgle bliver den delte "secret" for sessionen.
Initiatoren krypterer derefter denne AES-nøgle ved hjælp af modtagerens offentlige RSA-nøgle. Denne kryptering udføres ved at kalde window.crypto.subtle.encrypt (med RSA-OAEP-parametre) via JS Interop.

Overførsel af Krypteret Nøgle: Den resulterende RSA-krypterede AES-nøgle sendes til det simple backend API, adresseret til modtageren.

Dekryptering af Nøgle: Modtagerens applikation poller backend API'et. Når den henter den krypterede AES-nøgle, bruger den sin egen private RSA-nøgle til at dekryptere den. Denne dekryptering udføres ved at kalde window.crypto.subtle.decrypt (med RSA-OAEP-parametre) via JS Interop.

Resultat: Begge brugere har nu den identiske 256-bit AES-nøgle, som blev udvekslet uden nogensinde at sende selve nøglen i tekst. Dette sikrer confidentiality.

Beskedkommunikation (Confidentiality og Integrity):
Når den delte AES-nøgle er etableret, bliver beskeder beskyttet ved hjælp af AES i Galois/Counter Mode (AES-GCM).

Valg af AES-GCM: Jeg har valgt den fordi det er en Authenticated Encryption with Associated Data (AEAD) ciffer. Dette betyder at den giver både confidentiality gennem kryptering og integritet/autenticitet gennem et indbygget autentificerings-tag. Det undgår behovet for en separat HMAC-beregning (som i AES-CBC + HMAC).

Kryptering:
Når en besked sendes genereres en unik 12-byte (96-bit) IV (Initialiseringsvektor) ved hjælp af C#'s RandomNumberGenerator.GetBytes(12).

Beskedens tekst, den delte AES-nøgle og den unikke IV sendes til window.crypto.subtle.encrypt-funktionen (konfigureret til AES-GCM) via JS Interop.

Den resulterende ciffertekst (som inkluderer autentificerings-tagget) og IV'ens Base64-kode sendes til backend API'et.

Dekryptering & Integritetsverifikation:
Når et klient modtager en krypteret besked (IV + ciffertekst) fra API'et, sender den disse komponenter sammen med den delte AES-nøgle til window.crypto.subtle.decrypt-funktionen (konfigureret til AES-GCM) via JS Interop.

Web Crypto API'ets AES-GCM dekrypteringsproces verificerer automatisk integriteten af dataene ved hjælp af autentificerings-tagget indlejret i cifferteksten.

Hvis cifferteksten er blevet manipuleret, eller hvis den forkerte nøgle/IV blev brugt, vil decrypt-funktionen fejle og throw en exception. Applikationen fanger denne fejl (JSException) og kasserer beskeden hvilket sikrer integritet.

Hvis dekrypteringen lykkes, returneres den oprindelige klartekst, hvilket sikrer confidentiality.



Opsummering:
Applikationen bruger RSA (via JS Interop) til sikker nøgleudveksling og AES-GCM (via JS Interop) til fortrolig og integritetsbeskyttet beskedoverførsel.

Screenshots
Billede 1: JWK nøgle fra bruger 2 -> Set their key
![image](https://github.com/user-attachments/assets/aa49f25d-ed98-4a67-b1c3-2a42d7e9c4e7)

Billede 2: Billede 1 resultat
![image](https://github.com/user-attachments/assets/54877668-8c73-45d5-a596-cfb2e0fbf734)

Billede 3: Efter klik på initiate session fra bruger 1, Nøgle sendt til bruger 2 + "Shared Secret" Established som initiator (reciever hos bruger 2)
![image](https://github.com/user-attachments/assets/3d359aae-1516-4651-a43e-ffb7794b197b)

Billede 4 + 5: Bruger 1 har sendt besked, bruger 2 har modtaget og sendt tilbage
![image](https://github.com/user-attachments/assets/91a83994-b58a-454b-864c-6347774e05bb)
![image](https://github.com/user-attachments/assets/d0ee621f-e573-4631-a689-53e658095a37)
