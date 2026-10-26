import { useLogout } from '../auth/useAuth.ts';

function MainPage() {
    const logout = useLogout();
    
    return (
        <div className="main-page">
            <h1>Docsum</h1>
            <p>Welcome to the application</p>
            <nav>
                <a href="/login">Login</a>
                <button onClick={() => logout.mutate()}>Logout</button>
            </nav>
        </div>
    );
}

export default MainPage;