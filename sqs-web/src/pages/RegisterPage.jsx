import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../services/api';
import { User, Lock, Mail } from 'lucide-react';

const RegisterPage = () => {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleRegister = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(false);

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      setLoading(false);
      return;
    }

    if (password.length < 6) {
      setError('Mật khẩu phải từ 6 ký tự trở lên.');
      setLoading(false);
      return;
    }

    try {
      await api.post('/auth/register', { name, email, password, confirmPassword });
      setSuccess(true);
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err) {
      setError(err.response?.data?.message || 'Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' }}>
      <div className="glass-panel" style={{ width: '100%', maxWidth: '400px', padding: '3rem 2rem' }}>
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <h1 className="text-gradient" style={{ fontSize: '2rem', fontWeight: 700, marginBottom: '0.5rem' }}>Đăng Ký</h1>
          <p style={{ color: 'var(--text-secondary)' }}>Tạo tài khoản Khách hàng mới</p>
        </div>

        {error && (
          <div style={{ background: 'rgba(239, 68, 68, 0.1)', color: 'var(--danger)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1.5rem', fontSize: '0.9rem' }}>
            {error}
          </div>
        )}

        {success && (
          <div style={{ background: 'rgba(16, 185, 129, 0.1)', color: 'var(--success)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1.5rem', fontSize: '0.9rem' }}>
            Đăng ký thành công! Đang chuyển hướng đến đăng nhập...
          </div>
        )}

        <form onSubmit={handleRegister}>
          <div style={{ marginBottom: '1.5rem', position: 'relative' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Họ và tên</label>
            <div style={{ position: 'relative' }}>
              <User size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input 
                type="text" 
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="Nguyễn Văn A"
                required
                style={{ paddingLeft: '3rem' }}
              />
            </div>
          </div>

          <div style={{ marginBottom: '1.5rem', position: 'relative' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Email</label>
            <div style={{ position: 'relative' }}>
              <Mail size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input 
                type="email" 
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="email@example.com"
                required
                style={{ paddingLeft: '3rem' }}
              />
            </div>
          </div>

          <div style={{ marginBottom: '1.5rem', position: 'relative' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Mật khẩu</label>
            <div style={{ position: 'relative' }}>
              <Lock size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input 
                type="password" 
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                minLength={6}
                style={{ paddingLeft: '3rem' }}
              />
            </div>
          </div>

          <div style={{ marginBottom: '2rem', position: 'relative' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Xác nhận mật khẩu</label>
            <div style={{ position: 'relative' }}>
              <Lock size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input 
                type="password" 
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                required
                minLength={6}
                style={{ paddingLeft: '3rem' }}
              />
            </div>
          </div>

          <button 
            type="submit" 
            disabled={loading || success}
            style={{ 
              width: '100%', 
              padding: '1rem', 
              background: 'var(--accent-gradient)', 
              color: 'white', 
              borderRadius: 'var(--radius-md)', 
              fontWeight: 600, 
              fontSize: '1.1rem',
              opacity: loading ? 0.7 : 1,
              boxShadow: 'var(--shadow-glow)'
            }}
          >
            {loading ? 'Đang xử lý...' : 'ĐĂNG KÝ'}
          </button>
        </form>

        <div style={{ marginTop: '1.5rem', textAlign: 'center' }}>
          <p style={{ color: 'var(--text-secondary)' }}>
            Đã có tài khoản?{' '}
            <Link to="/login" style={{ color: 'var(--accent-primary)', fontWeight: 500 }}>
              Đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
