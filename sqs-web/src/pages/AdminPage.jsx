import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import Navbar from '../components/Navbar';
import { BarChart3, Users, Activity, Target } from 'lucide-react';

const AdminPage = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!user || user.role !== 'Admin') {
      navigate('/login');
      return;
    }
    loadStats();
  }, [user]);

  const loadStats = async () => {
    try {
      const res = await api.get('/admin/stats');
      setStats(res.data);
    } catch (err) {
      setError('Không thể tải thống kê');
    } finally {
      setLoading(false);
    }
  };

  if (!user || user.role !== 'Admin') return null;

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <Navbar title="Admin Dashboard" user={user} onLogout={onLogout} />
      
      {error && <div style={{ color: 'var(--danger)', marginBottom: '1rem', background: 'rgba(239, 68, 68, 0.1)', padding: '1rem', borderRadius: 'var(--radius-md)' }}>{error}</div>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '1.5rem', marginBottom: '2rem' }}>
        {/* KPI Cards */}
        <StatCard icon={<Users />} title="Khách Đang Đợi" value={stats?.totalWaiting || 0} color="var(--warning)" />
        <StatCard icon={<Activity />} title="Đang Phục Vụ" value={stats?.totalCalling || 0} color="var(--accent-primary)" />
        <StatCard icon={<CheckCircle />} title="Đã Hoàn Thành" value={stats?.totalCompleted || 0} color="var(--success)" />
        <StatCard icon={<Target />} title="Đã Hủy/Bỏ Qua" value={stats?.totalCanceled || 0} color="var(--danger)" />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem' }}>
        {/* Staff KPIs */}
        <div className="glass-panel" style={{ padding: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
            <BarChart3 color="var(--accent-primary)" />
            <h3 style={{ fontSize: '1.25rem' }}>Bảng xếp hạng Staff (KPI)</h3>
          </div>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {stats?.staffKpis?.length > 0 ? stats.staffKpis.map((staff, index) => (
              <div key={staff.staffId} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-md)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                  <div style={{ width: '30px', height: '30px', borderRadius: '50%', background: index === 0 ? 'var(--warning)' : 'var(--bg-secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'bold' }}>{index + 1}</div>
                  <div>
                    <div style={{ fontWeight: 600 }}>{staff.staffName}</div>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Quầy {staff.counterId || 'N/A'}</div>
                  </div>
                </div>
                <div style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--success)' }}>
                  {staff.kpi} pts
                </div>
              </div>
            )) : <div style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '2rem' }}>Chưa có dữ liệu KPI</div>}
          </div>
        </div>

        {/* Services Stats */}
        <div className="glass-panel" style={{ padding: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
            <Activity color="var(--accent-primary)" />
            <h3 style={{ fontSize: '1.25rem' }}>Trạng thái Dịch vụ</h3>
          </div>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {stats?.serviceStats?.length > 0 ? stats.serviceStats.map(s => (
              <div key={s.serviceId} style={{ padding: '1.5rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-md)', borderLeft: '3px solid var(--accent-primary)' }}>
                <h4 style={{ fontSize: '1.1rem', marginBottom: '0.5rem' }}>{s.serviceName}</h4>
                <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                  <span>Đợi: <strong style={{ color: 'var(--warning)' }}>{s.waiting}</strong></span>
                  <span>Đang gọi: <strong style={{ color: 'var(--accent-primary)' }}>{s.calling}</strong></span>
                  <span>Xong: <strong style={{ color: 'var(--success)' }}>{s.completed}</strong></span>
                </div>
              </div>
            )) : <div style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '2rem' }}>Chưa có dữ liệu dịch vụ</div>}
          </div>
        </div>
      </div>
    </div>
  );
};

// Helper components
const CheckCircle = (props) => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
);

const StatCard = ({ icon, title, value, color }) => (
  <div className="glass-panel" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column', borderTop: `3px solid ${color}` }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
      {React.cloneElement(icon, { size: 20, color })}
      <span style={{ fontSize: '0.9rem', fontWeight: 500 }}>{title}</span>
    </div>
    <div style={{ fontSize: '2.5rem', fontWeight: 700, color: 'white', lineHeight: 1 }}>{value}</div>
  </div>
);

export default AdminPage;
