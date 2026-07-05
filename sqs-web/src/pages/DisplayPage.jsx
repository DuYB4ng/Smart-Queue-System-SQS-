import React, { useState, useEffect } from 'react';
import api from '../services/api';
import signalRService from '../services/signalr';
import { Volume2, Monitor } from 'lucide-react';

const DisplayPage = () => {
  const [services, setServices] = useState([]);
  const [queues, setQueues] = useState({});
  const [recentCall, setRecentCall] = useState(null);

  useEffect(() => {
    loadAllQueues();
    signalRService.connect().then(() => {
      signalRService.joinGroup('display');
    });

    signalRService.on('TicketCalled', (payload) => {
      setRecentCall(payload);
      playDingSound();
      
      // Clear recent call highlight after 10s
      setTimeout(() => {
        setRecentCall(prev => prev?.ticketId === payload.ticketId ? null : prev);
      }, 10000);
      
      // Update the specific queue
      loadQueueForService(payload.serviceId);
    });

    signalRService.on('QueueUpdated', (payload) => {
      setQueues(prev => ({
        ...prev,
        [payload.serviceId]: {
          ...prev[payload.serviceId],
          waitingCount: payload.waitingCount,
          currentCalling: payload.currentCalling
        }
      }));
    });

    return () => {
      signalRService.leaveGroup('display');
      signalRService.off('TicketCalled');
      signalRService.off('QueueUpdated');
    };
  }, []);

  const loadAllQueues = async () => {
    try {
      const res = await api.get('/services');
      const srvs = res.data;
      setServices(srvs);
      
      // Load queue for each service
      for (const s of srvs) {
        loadQueueForService(s.id);
      }
    } catch (err) {
      console.error('Error loading services for display', err);
    }
  };

  const loadQueueForService = async (serviceId) => {
    try {
      const res = await api.get(`/tickets/queue?serviceId=${serviceId}`);
      setQueues(prev => ({
        ...prev,
        [serviceId]: res.data
      }));
    } catch (err) {
      console.error(`Error loading queue for service ${serviceId}`, err);
    }
  };

  const playDingSound = () => {
    // Basic ding sound using Web Audio API
    try {
      const AudioContext = window.AudioContext || window.webkitAudioContext;
      const ctx = new AudioContext();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      
      osc.connect(gain);
      gain.connect(ctx.destination);
      
      osc.type = 'sine';
      osc.frequency.setValueAtTime(880, ctx.currentTime); // A5
      osc.frequency.exponentialRampToValueAtTime(440, ctx.currentTime + 0.5); // A4
      
      gain.gain.setValueAtTime(1, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 1);
      
      osc.start();
      osc.stop(ctx.currentTime + 1);
    } catch (e) {
      console.log('Audio playback failed', e);
    }
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: 'var(--bg-primary)' }}>
      {/* Header */}
      <header className="glass-panel" style={{ padding: '1.5rem 3rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '2rem', borderRadius: 'var(--radius-lg)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <Monitor size={32} color="var(--accent-primary)" />
          <h1 style={{ fontSize: '2rem', fontWeight: 700, margin: 0 }}>HỆ THỐNG XẾP HÀNG TỰ ĐỘNG</h1>
        </div>
        <div style={{ fontSize: '2rem', fontWeight: 300, color: 'var(--accent-primary)' }}>
          {new Date().toLocaleTimeString('vi-VN')}
        </div>
      </header>

      {/* Main Content */}
      <div style={{ display: 'flex', flex: 1, padding: '0 2rem 2rem 2rem', gap: '2rem' }}>
        
        {/* Left Side: Recent Call Announcement */}
        <div className="glass-panel" style={{ flex: '1 1 40%', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '3rem', position: 'relative', overflow: 'hidden' }}>
          {recentCall ? (
            <>
              <div style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, background: 'var(--accent-gradient)', opacity: 0.1, animation: 'pulse 2s infinite' }} />
              <Volume2 size={64} color="var(--accent-primary)" style={{ animation: 'bounce 1s infinite', marginBottom: '2rem' }} />
              <h2 style={{ fontSize: '2.5rem', color: 'var(--text-secondary)' }}>XIN MỜI KHÁCH HÀNG SỐ</h2>
              <div style={{ fontSize: '10rem', fontWeight: 800, color: 'var(--accent-primary)', lineHeight: 1, margin: '2rem 0', textShadow: 'var(--shadow-glow)' }}>
                {recentCall.ticketNumber}
              </div>
              <h2 style={{ fontSize: '3rem', color: 'white' }}>ĐẾN QUẦY <span className="text-gradient">{recentCall.counterName}</span></h2>
              <p style={{ fontSize: '1.5rem', color: 'var(--text-secondary)', marginTop: '1rem' }}>Dịch vụ: {recentCall.serviceName}</p>
            </>
          ) : (
            <div style={{ textAlign: 'center', opacity: 0.5 }}>
              <Monitor size={80} style={{ marginBottom: '2rem' }} />
              <h2 style={{ fontSize: '2rem' }}>Chờ gọi số...</h2>
            </div>
          )}
        </div>

        {/* Right Side: Grid of Services */}
        <div style={{ flex: '1 1 60%', display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '1.5rem', alignContent: 'start' }}>
          {services.map(s => {
            const q = queues[s.id] || { currentCalling: '--', waitingCount: 0 };
            return (
              <div key={s.id} className="glass-panel" style={{ padding: '2rem', display: 'flex', flexDirection: 'column' }}>
                <h3 style={{ fontSize: '1.5rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>{s.name}</h3>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginTop: 'auto' }}>
                  <div>
                    <div style={{ fontSize: '1rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '2px' }}>Đang gọi</div>
                    <div style={{ fontSize: '4rem', fontWeight: 700, color: 'white', lineHeight: 1 }}>{q.currentCalling || '--'}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div style={{ fontSize: '1rem', color: 'var(--text-secondary)' }}>Đang chờ</div>
                    <div style={{ fontSize: '2rem', fontWeight: 600, color: 'var(--warning)' }}>{q.waitingCount}</div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
      
      <style>{`
        @keyframes pulse {
          0% { opacity: 0.1; }
          50% { opacity: 0.2; }
          100% { opacity: 0.1; }
        }
        @keyframes bounce {
          0%, 100% { transform: translateY(0); }
          50% { transform: translateY(-10px); }
        }
      `}</style>
    </div>
  );
};

export default DisplayPage;
