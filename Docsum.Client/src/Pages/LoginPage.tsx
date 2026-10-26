import { useLogin } from '../auth/useAuth.ts';
import { useState } from 'react';
import styles from './LoginPage.module.css';

function LoginPage() {
    const [email, setEmail] = useState<string>('');
    const [password, setPassword] = useState<string>('');

    const mutation = useLogin();


    const handleSubmit: React.SubmitEventHandler<HTMLFormElement> = (e) => {
        e.preventDefault();
        mutation.mutate({ email, password });
    };

    return (
        <div className={styles.loginPage}>
            <form className={styles.form} onSubmit={handleSubmit}>
                <h2 className={styles.title}>Welcome Back</h2>

                {mutation.isError && (
                    <div className={styles.error}>
                        {mutation.error.message}
                    </div>
                )}

                <input
                    className={styles.input}
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    required
                />
                <input
                    className={styles.input}
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    required
                />
                <button className={styles.button} type="submit" disabled={mutation.isPending}>
                    {mutation.isPending ? 'Logging in...' : 'Login'}
                </button>
            </form>
        </div>
    );
}

export default LoginPage;