import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './useAuth';

export default function ProtectedRoute() {
    const { user, isLoading, isError } = useAuth();
    const location = useLocation();

    if (isLoading) return <div>Loading...</div>;
    if (isError) return <div>Server unavailable. Please try again later.</div>;
    if (!user) return <Navigate to="/login" replace state={{ from: location }} />;

    return <Outlet />;
}