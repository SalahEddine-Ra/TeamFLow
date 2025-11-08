import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
// import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <div className="p-8 text-xl text-red-600">Tailwind work</div>
  </StrictMode>,
)
