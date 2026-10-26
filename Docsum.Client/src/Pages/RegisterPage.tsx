import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from './LoginPage.module.css';

type LoginPageProps = {
    email: string;
    password: string;
};

function RegisterPage() {
    const navigate = useNavigate();

    const [email, setEmail] = useState<string>('');
    const [password, setPassword] = useState<string>('');

    const loginUser = async (data: LoginPageProps) => {
        const res = await fetch('/api/auth/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });

        if (!res.ok) {
            throw new Error('Невірний email або пароль');
        }

        return res.json();
    };

    const mutation = useMutation({
        mutationFn: loginUser,
        onSuccess: (data) => {
            if (data?.token) {
                localStorage.setItem('token', data.token);
            }
            navigate('/');
        },
    });

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
                    {mutation.isPending ? 'Registration...' : 'Register'}
                </button>
            </form>
        </div>
    );
}

export default RegisterPage;