import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import { RouterProvider, createBrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { LoginPage, RegisterPage, LibraryHomePage, DocumentPage, ErrorPage } from "./Pages";
import ProtectedRoute from './auth/ProtectedRoute';
import LibraryLayout from './layout/LibraryLayout.tsx';

const queryClient = new QueryClient();
const router = createBrowserRouter([
    {
        path: '/',
        element: <App />,
        errorElement: <ErrorPage />,
        children: [
            { path: 'login', element: <LoginPage /> },
            { path: 'register', element: <RegisterPage /> },
            {
                element: <ProtectedRoute />,
                children: [
                    {
                        element: <LibraryLayout />,
                        children: [
                            { index: true, element: <LibraryHomePage /> },
                            { path: 'documents/:id', element: <DocumentPage /> },
                        ],
                    },
                ],
            },
        ],
    },
]);

createRoot(document.getElementById('root')!).render(
    <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
    </QueryClientProvider>
)
