import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { User, Mail, MapPin, Calendar, Lock, Save } from 'lucide-react';

const ProfilePage = ({ user, onLogout }) => {
  const [profile, setProfile] = useState({
    name: '',
    email: '',
    birthday: '',
    address: ''
  });
  
  const [passwords, setPasswords] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [pwdError, setPwdError] = useState(null);
  const [pwdSuccess, setPwdSuccess] = useState(null);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await api.get('/users/me');
        const data = res.data;
        setProfile({
          name: data.name || '',
          email: data.email || '',
          birthday: data.birthday ? data.birthday.split('T')[0] : '',
          address: data.address || ''
        });
      } catch (err) {
        setError('Không thể tải thông tin hồ sơ.');
      }
    };
    fetchProfile();
  }, []);

  const handleUpdateProfile = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      await api.put('/users/me', {
        name: profile.name,
        birthday: profile.birthday || null,
        address: profile.address
      });
      setSuccess('Cập nhật hồ sơ thành công!');
    } catch (err) {
      setError(err.response?.data?.message || 'Cập nhật thất bại.');
    } finally {
      setLoading(false);
    }
  };

  const handleChangePassword = async (e) => {
    e.preventDefault();
    setLoading(true);
    setPwdError(null);
    setPwdSuccess(null);

    if (passwords.newPassword !== passwords.confirmPassword) {
      setPwdError('Mật khẩu mới không khớp.');
      setLoading(false);
      return;
    }

    try {
      await api.put('/users/me/password', {
        currentPassword: passwords.currentPassword,
        newPassword: passwords.newPassword,
        confirmPassword: passwords.confirmPassword
      });
      setPwdSuccess('Đổi mật khẩu thành công! Vui lòng đăng nhập lại.');
      setPasswords({ currentPassword: '', newPassword: '', confirmPassword: '' });
      setTimeout(() => onLogout(), 3000);
    } catch (err) {
      setPwdError(err.response?.data?.message || 'Đổi mật khẩu thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto', display: 'flex', flexDirection: window.innerWidth < 768 ? 'column' : 'row', gap: '2rem' }}>
      
      {/* Cập nhật thông tin */}
      <div className="glass-panel" style={{ flex: 1, padding: '2rem' }}>
        <h2 style={{ fontSize: '1.5rem', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <User className="text-gradient" /> Thông tin cá nhân
        </h2>
        
        {error && <div style={{ background: 'rgba(239,68,68,0.1)', color: 'var(--danger)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1rem' }}>{error}</div>}
        {success && <div style={{ background: 'rgba(16,185,129,0.1)', color: 'var(--success)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1rem' }}>{success}</div>}

        <form onSubmit={handleUpdateProfile}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Họ và tên</label>
            <div style={{ position: 'relative' }}>
              <User size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input type="text" value={profile.name} onChange={e => setProfile({...profile, name: e.target.value})} style={{ paddingLeft: '2.5rem' }} required />
            </div>
          </div>
          
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Email (Không thể đổi)</label>
            <div style={{ position: 'relative' }}>
              <Mail size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input type="email" value={profile.email} disabled style={{ paddingLeft: '2.5rem', opacity: 0.7 }} />
            </div>
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Ngày sinh</label>
            <div style={{ position: 'relative' }}>
              <Calendar size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input type="date" value={profile.birthday} onChange={e => setProfile({...profile, birthday: e.target.value})} style={{ paddingLeft: '2.5rem' }} />
            </div>
          </div>

          <div style={{ marginBottom: '2rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Địa chỉ</label>
            <div style={{ position: 'relative' }}>
              <MapPin size={18} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              <input type="text" value={profile.address} onChange={e => setProfile({...profile, address: e.target.value})} style={{ paddingLeft: '2.5rem' }} placeholder="Địa chỉ thường trú" />
            </div>
          </div>

          <button type="submit" disabled={loading} style={{ width: '100%', padding: '0.75rem', background: 'var(--accent-primary)', color: 'white', borderRadius: 'var(--radius-md)', display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '0.5rem' }}>
            <Save size={18} /> Lưu thay đổi
          </button>
        </form>
      </div>

      {/* Đổi mật khẩu */}
      <div className="glass-panel" style={{ flex: 1, padding: '2rem', height: 'fit-content' }}>
        <h2 style={{ fontSize: '1.5rem', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Lock className="text-gradient" /> Đổi mật khẩu
        </h2>

        {pwdError && <div style={{ background: 'rgba(239,68,68,0.1)', color: 'var(--danger)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1rem' }}>{pwdError}</div>}
        {pwdSuccess && <div style={{ background: 'rgba(16,185,129,0.1)', color: 'var(--success)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1rem' }}>{pwdSuccess}</div>}

        <form onSubmit={handleChangePassword}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Mật khẩu hiện tại</label>
            <input type="password" value={passwords.currentPassword} onChange={e => setPasswords({...passwords, currentPassword: e.target.value})} required />
          </div>
          
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Mật khẩu mới</label>
            <input type="password" value={passwords.newPassword} onChange={e => setPasswords({...passwords, newPassword: e.target.value})} required minLength={6} />
          </div>

          <div style={{ marginBottom: '2rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Xác nhận mật khẩu mới</label>
            <input type="password" value={passwords.confirmPassword} onChange={e => setPasswords({...passwords, confirmPassword: e.target.value})} required minLength={6} />
          </div>

          <button type="submit" disabled={loading} style={{ width: '100%', padding: '0.75rem', background: 'rgba(0,0,0,0.05)', border: '1px solid var(--border-light)', color: 'var(--text-primary)', borderRadius: 'var(--radius-md)', display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '0.5rem' }}>
            <Lock size={18} /> Cập nhật mật khẩu
          </button>
        </form>
      </div>

    </div>
  );
};

export default ProfilePage;
