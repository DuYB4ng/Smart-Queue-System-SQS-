import React, { useState, useEffect } from 'react';
import api from '../services/api';
import signalRService from '../services/signalr';

const KioskPage = () => {
  const [services, setServices] = useState([]);
  const [guestName, setGuestName] = useState('');
  const [selectedService, setSelectedService] = useState(null);
  const [ticketResult, setTicketResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadServices();
    signalRService.connect();
    
    // Auto refresh queue count when updated
    signalRService.on('QueueUpdated', (payload) => {
      setServices(prev => prev.map(s => 
        s.id === payload.serviceId 
          ? { ...s, waitingCount: payload.waitingCount } 
          : s
      ));
    });

    return () => {
      signalRService.off('QueueUpdated');
    };
  }, []);

  const loadServices = async () => {
    try {
      const res = await api.get('/services');
      setServices(res.data);
      setLoading(false);
    } catch (err) {
      setError('Không thể tải danh sách dịch vụ.');
      setLoading(false);
    }
  };

  const handleCreateTicket = async (e) => {
    e.preventDefault();
    if (!selectedService || !guestName.trim()) return;

    try {
      const res = await api.post('/tickets/guest', {
        serviceId: selectedService.id,
        guestName: guestName.trim()
      });
      setTicketResult(res.data);
      setGuestName('');
      setSelectedService(null);
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi lấy số');
    }
  };

  if (loading) return <div style={{ textAlign: 'center', marginTop: '2rem' }}>Đang tải hệ thống Kiosk...</div>;

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto', padding: '2rem' }}>
      <div className="glass-panel" style={{ padding: '3rem', textAlign: 'center' }}>
        <h1 className="text-gradient" style={{ fontSize: '2.5rem', marginBottom: '1rem', fontWeight: 700 }}>LẤY SỐ THỨ TỰ</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '3rem' }}>Vui lòng nhập tên và chọn dịch vụ bạn cần</p>

        {error && <div style={{ color: 'var(--danger)', marginBottom: '1rem', background: 'rgba(239, 68, 68, 0.1)', padding: '1rem', borderRadius: 'var(--radius-md)' }}>{error}</div>}

        {ticketResult ? (
          <div style={{ animation: 'float 2s ease-in-out infinite alternate', background: 'var(--accent-gradient)', padding: '2rem', borderRadius: 'var(--radius-lg)', color: 'white' }}>
            <h2 style={{ fontSize: '1.5rem', opacity: 0.9 }}>Số của bạn là:</h2>
            <div style={{ fontSize: '5rem', fontWeight: 800, margin: '1rem 0' }}>{ticketResult.ticketNumber}</div>
            <p style={{ fontSize: '1.25rem' }}>Dịch vụ: {ticketResult.serviceName}</p>
            <p style={{ marginTop: '1rem', opacity: 0.8 }}>Số người đang chờ trước bạn: {ticketResult.estimatedWait}</p>
            
            <button 
              onClick={() => setTicketResult(null)}
              style={{ marginTop: '2rem', background: 'white', color: 'var(--accent-primary)', padding: '0.75rem 2rem', borderRadius: 'var(--radius-full)', fontWeight: 600, fontSize: '1.1rem' }}
            >
              Lấy số khác
            </button>
          </div>
        ) : (
          <form onSubmit={handleCreateTicket} style={{ textAlign: 'left' }}>
            <div style={{ marginBottom: '2rem' }}>
              <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Họ và tên của bạn</label>
              <input 
                type="text" 
                placeholder="Nhập tên..." 
                value={guestName}
                onChange={(e) => setGuestName(e.target.value)}
                required
                style={{ fontSize: '1.25rem', padding: '1rem' }}
              />
            </div>

            <label style={{ display: 'block', marginBottom: '1rem', color: 'var(--text-secondary)' }}>Chọn dịch vụ</label>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
              {services.map(s => (
                <div 
                  key={s.id}
                  onClick={() => setSelectedService(s)}
                  style={{ 
                    padding: '1.5rem', 
                    borderRadius: 'var(--radius-md)', 
                    border: `2px solid ${selectedService?.id === s.id ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                    background: selectedService?.id === s.id ? 'rgba(99, 102, 241, 0.1)' : 'rgba(0,0,0,0.2)',
                    cursor: 'pointer',
                    transition: 'var(--transition-fast)'
                  }}
                >
                  <h3 style={{ fontSize: '1.25rem', color: selectedService?.id === s.id ? 'var(--accent-primary)' : 'white' }}>{s.name}</h3>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginTop: '0.5rem' }}>Đang đợi: {s.waitingCount ?? 0} người</p>
                </div>
              ))}
            </div>

            <button 
              type="submit" 
              disabled={!selectedService || !guestName.trim()}
              style={{ 
                width: '100%', 
                padding: '1rem', 
                background: (!selectedService || !guestName.trim()) ? 'var(--bg-secondary)' : 'var(--accent-gradient)',
                color: (!selectedService || !guestName.trim()) ? 'var(--text-secondary)' : 'white',
                borderRadius: 'var(--radius-md)',
                fontSize: '1.25rem',
                fontWeight: 600,
                cursor: (!selectedService || !guestName.trim()) ? 'not-allowed' : 'pointer',
                boxShadow: (!selectedService || !guestName.trim()) ? 'none' : 'var(--shadow-glow)'
              }}
            >
              IN SỐ THỨ TỰ
            </button>
          </form>
        )}
      </div>
    </div>
  );
};

export default KioskPage;
