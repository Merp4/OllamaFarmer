import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// import 'mdb-react-ui-kit/dist/css/mdb.min.css';
import "@fortawesome/fontawesome-free/css/all.min.css";
import 'react-toastify/dist/ReactToastify.css';
//import './themes/main.theme.scss';
import './index.scss'
//import 'bootstrap';
// import './themes/main.theme.scss'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router';
import { router } from './Routing.tsx';


const queryClient = new QueryClient()


createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <QueryClientProvider client={queryClient}>
            <RouterProvider router={router} />
        </QueryClientProvider>
    </StrictMode>,
)
