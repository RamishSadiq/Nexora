# Nexora web application

The Next.js frontend for the standalone Nexora platform. Run commands from this directory:

```powershell
npm ci
npm run dev
```

The application uses `http://localhost:5080` for the Nexora API by default. Set `NEXORA_API_URL` before building and starting when using another API origin. Start the API using the instructions in the repository root README.

```powershell
npm run lint
npm run build
npm start
```

Authentication uses the API's cookie session. Local development sign-in is available only when enabled by the Development API configuration; Microsoft Entra credentials are not required for local development.
