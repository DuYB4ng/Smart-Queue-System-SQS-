import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import signalRService from '../services/signalr';
import Navbar from '../components/Navbar';
import { CheckCircle, SkipForward, Megaphone, Users } from 'lucide-react';

const StaffPage = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const [currentTicket, setCurrentTicket] = useState(null);
  const [queue, setQueue] = useState({ waitingCount: 0, waitingList: [] });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  // Dynamic counterId based on user ID (Staff1..5 have User ID 2..6 and map to Counters 1..5)
  const counterId = user ? (parseInt(user.id) - 1) : 1;
  const serviceId = counterId; // Based on 1-1 mapping in database

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    
    loadMyQueue();
    
    signalRService.connect().then(() => {
      signalRService.joinGroup(`staff-${counterId}`);
      // Join the corresponding service group to receive real-time queue updates
      signalRService.joinGroup(`service-${serviceId}`);
    });

    signalRService.on('QueueUpdated', (payload) => {
      loadMyQueue();
    });

    return () => {
      signalRService.leaveGroup(`staff-${counterId}`);
      signalRService.leaveGroup(`service-${serviceId}`);
      signalRService.off('QueueUpdated');
    };
  }, [user]);

  const loadMyQueue = async () => {
    try {
      const res = await api.get(`/staff/my-queue?counterId=${counterId}`);
      setQueue(res.data);
    } catch (err) {
      console.error('Failed to load queue', err);
    }
  };

  const handleCallNext = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post('/staff/call-next', { counterId });
      setCurrentTicket(res.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể gọi số');
    } finally {
      setLoading(false);
    }
  };

  const handleComplete = async () => {
    if (!currentTicket) return;
    setLoading(true);
    try {
      await api.post(`/staff/complete/${currentTicket.ticketId}`);
      setCurrentTicket(null);
      loadMyQueue();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi hoàn thành');
    } finally {
      setLoading(false);
    }
  };

  const handleSkip = async () => {
    if (!currentTicket) return;
    setLoading(true);
    try {
      await api.post(`/staff/skip/${currentTicket.ticketId}`);
      setCurrentTicket(null);
      loadMyQueue();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi bỏ qua');
    } finally {
      setLoading(false);
    }
  };

  if (!user) return null;

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <Navbar title="Staff Dashboard" user={user} onLogout={onLogout} />
      
      {error && <div style={{ color: 'var(--danger)', marginBottom: '1rem', background: 'rgba(239, 68, 68, 0.1)', padding: '1rem', borderRadius: 'var(--radius-md)' }}>{error}</div>}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '2rem' }}>
        
        {/* Main Workspace */}
        <div className="glass-panel" style={{ padding: '2rem', display: 'flex', flexDirection: 'column' }}>
          <h2 style={{ fontSize: '1.5rem', marginBottom: '2rem', color: 'var(--text-secondary)' }}>Phiên làm việc hiện tại (Quầy {counterId})</h2>
          
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
            {currentTicket ? (
              <div style={{ textAlign: 'center', width: '100%' }}>
                <div style={{ fontSize: '1.25rem', color: 'var(--accent-primary)', marginBottom: '1rem' }}>ĐANG PHỤC VỤ</div>
                <div style={{ fontSize: '6rem', fontWeight: 800, lineHeight: 1, textShadow: 'var(--shadow-glow)' }}>{currentTicket.ticketNumber}</div>
                <div style={{ fontSize: '1.5rem', color: 'white', marginTop: '1rem' }}>Khách: {currentTicket.customerName}</div>
                <div style={{ fontSize: '1.25rem', color: 'var(--text-secondary)' }}>Dịch vụ: {currentTicket.serviceName}</div>
                
                <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center', marginTop: '3rem' }}>
                  <button 
                    onClick={handleComplete}
                    disabled={loading}
                    style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '1rem 2rem', background: 'var(--success)', color: 'white', borderRadius: 'var(--radius-full)', fontSize: '1.1rem', fontWeight: 600 }}
                  >
                    <CheckCircle size={20} />
                    Hoàn thành (+1 KPI)
                  </button>
                  <button 
                    onClick={handleSkip}
                    disabled={loading}
                    style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '1rem 2rem', background: 'var(--bg-secondary)', border: '1px solid var(--border-light)', color: 'var(--text-secondary)', borderRadius: 'var(--radius-full)', fontSize: '1.1rem' }}
                  >
                    <SkipForward size={20} />
                    Bỏ qua (Khách vắng)
                  </button>
                </div>
              </div>
            ) : (
              <div style={{ textAlign: 'center' }}>
                <Megaphone size={64} style={{ color: 'var(--text-secondary)', marginBottom: '2rem', opacity: 0.5 }} />
                <h3 style={{ fontSize: '2rem', marginBottom: '1rem' }}>Quầy đang trống</h3>
                <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem' }}>Đang có {queue.waitingCount} người chờ trong hàng đợi</p>
                <button 
                  onClick={handleCallNext}
                  disabled={loading || queue.waitingCount === 0}
                  style={{ 
                    padding: '1rem 3rem', 
                    background: queue.waitingCount > 0 ? 'var(--accent-gradient)' : 'var(--bg-secondary)', 
                    color: queue.waitingCount > 0 ? 'white' : 'var(--text-secondary)', 
                    borderRadius: 'var(--radius-full)', 
                    fontSize: '1.25rem', 
                    fontWeight: 600,
                    boxShadow: queue.waitingCount > 0 ? 'var(--shadow-glow)' : 'none'
                  }}
                >
                  GỌI SỐ TIẾP THEO
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Sidebar: Queue Status */}
        <div className="glass-panel" style={{ padding: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem', borderBottom: '1px solid var(--border-light)', paddingBottom: '1rem' }}>
            <Users size={24} color="var(--accent-primary)" />
            <h3 style={{ fontSize: '1.25rem' }}>Hàng đợi ({queue.waitingCount})</h3>
          </div>
          
          {queue.waitingList.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {queue.waitingList.map((item, idx) => (
                <div key={item.ticketId} style={{ padding: '1rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-md)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderLeft: idx === 0 ? '3px solid var(--warning)' : '3px solid transparent' }}>
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '1.1rem' }}>{item.ticketNumber}</div>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>{item.customerName}</div>
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                    #{item.position}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ textAlign: 'center', color: 'var(--text-secondary)', padding: '2rem 0' }}>
              Không có khách hàng nào đang đợi
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default StaffPage;
