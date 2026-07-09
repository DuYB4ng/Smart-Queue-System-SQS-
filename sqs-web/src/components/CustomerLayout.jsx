import React from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { LogOut, User as UserIcon, Ticket, History, Settings } from 'lucide-react';

const CustomerLayout = ({ user, onLogout }) => {
  const location = useLocation();
  
  const navItems = [
    { path: '/', label: 'Lấy số / Đặt hẹn', icon: <Ticket size={18} /> },
    { path: '/my-tickets', label: 'Vé của tôi', icon: <History size={18} /> },
    { path: '/profile', label: 'Cài đặt hồ sơ', icon: <Settings size={18} /> }
  ];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <nav className="glass-panel" style={{ 
        padding: '1rem 2rem', 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        margin: '1rem',
        marginBottom: '2rem'
      }}>
        <div>
          <h1 className="text-gradient" style={{ fontSize: '1.5rem', fontWeight: 700 }}>SQS Customer</h1>
        </div>
        
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem' }}>
          {navItems.map(item => {
            const isActive = location.pathname === item.path;
            return (
              <Link 
                key={item.path} 
                to={item.path} 
                style={{ 
                  color: isActive ? 'var(--accent-primary)' : 'var(--text-secondary)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem',
                  fontWeight: isActive ? 600 : 400
                }}
              >
                {item.icon}
                <span>{item.label}</span>
              </Link>
            )
          })}
          
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginLeft: '1rem', borderLeft: '1px solid var(--border-light)', paddingLeft: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--accent-primary)' }}>
              <UserIcon size={18} />
              <span>{user?.name}</span>
            </div>
            <button 
              onClick={onLogout}
              style={{ background: 'transparent', color: 'var(--danger)', display: 'flex', alignItems: 'center', gap: '0.25rem' }}
            >
              <LogOut size={18} />
              <span>Thoát</span>
            </button>
          </div>
        </div>
      </nav>

      <main style={{ flex: 1, padding: '0 2rem 2rem' }}>
        <Outlet />
      </main>
    </div>
  );
};

export default CustomerLayout;
