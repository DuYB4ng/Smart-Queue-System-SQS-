import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { LogOut, User as UserIcon } from 'lucide-react';

const Navbar = ({ title, user, onLogout }) => {
  return (
    <nav className="glass-panel" style={{ padding: '1rem 2rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
      <div>
        <h1 className="text-gradient" style={{ fontSize: '1.5rem', fontWeight: 700 }}>{title || 'Smart Queue System'}</h1>
      </div>
      
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        <Link to="/" style={{ color: 'var(--text-secondary)' }}>Kiosk</Link>
        <Link to="/display" style={{ color: 'var(--text-secondary)' }}>Display</Link>
        <Link to="/staff" style={{ color: 'var(--text-secondary)' }}>Staff</Link>
        <Link to="/admin" style={{ color: 'var(--text-secondary)' }}>Admin</Link>
        
        {user ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginLeft: '1rem', borderLeft: '1px solid var(--border-light)', paddingLeft: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--accent-primary)' }}>
              <UserIcon size={18} />
              <span>{user.name}</span>
            </div>
            <button 
              onClick={onLogout}
              style={{ background: 'transparent', color: 'var(--danger)', display: 'flex', alignItems: 'center', gap: '0.25rem' }}
            >
              <LogOut size={18} />
              <span>Thoát</span>
            </button>
          </div>
        ) : (
          <Link to="/login" style={{ marginLeft: '1rem', padding: '0.5rem 1rem', background: 'var(--accent-primary)', color: 'white', borderRadius: 'var(--radius-md)' }}>
            Đăng nhập
          </Link>
        )}
      </div>
    </nav>
  );
};

export default Navbar;
