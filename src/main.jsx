import React from 'react'
import { createRoot } from 'react-dom/client'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import App from './App'
import './index.css'
import { msalConfig } from './msalConfig'

// const msalInstance = new PublicClientApplication(msalConfig)

createRoot(document.getElementById('root')).render(
  // <React.StrictMode>
    // <MsalProvider instance={msalInstance}>
      <App />
    // </MsalProvider>
  // </React.StrictMode>
)
