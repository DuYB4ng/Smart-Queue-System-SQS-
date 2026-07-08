import React, { useState, useEffect } from 'react';
import api from '../services/api';
import signalRService from '../services/signalr';
import { Volume2, Monitor } from 'lucide-react';

const DisplayPage = () => {
  const [services, setServices] = useState([]);
  const [queues, setQueues] = useState({});
  const [recentCall, setRecentCall] = useState(null);
  const [currentTime, setCurrentTime] = useState(new Date().toLocaleTimeString('vi-VN'));

  useEffect(() => {
    loadAllQueues();
    signalRService.connect().then(() => {
      signalRService.joinGroup('display');
    });

    const timer = setInterval(() => {
      setCurrentTime(new Date().toLocaleTimeString('vi-VN'));
    }, 1000);

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
      console.log('SignalR QueueUpdated payload:', payload);
      
      const sId = payload.serviceId || payload.ServiceId;
      if (!sId) return;

      // When queue is updated, reload the queue for that service to get the latest waitingList
      loadQueueForService(sId);
    });

    return () => {
      signalRService.leaveGroup('display');
      signalRService.off('TicketCalled');
      signalRService.off('QueueUpdated');
      clearInterval(timer);
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

  // Aggregate all waiting tickets from all queues and sort by createdAt
  const allWaitingNumbers = Object.values(queues)
    .flatMap(q => q.waitingList || [])
    .sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: 'var(--bg-primary)' }}>
      {/* Header */}
      <header className="glass-panel" style={{ padding: '1.5rem 3rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '2rem', borderRadius: 'var(--radius-lg)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <Monitor size={32} color="var(--accent-primary)" />
          <h1 style={{ fontSize: '2rem', fontWeight: 700, margin: 0 }}>HỆ THỐNG XẾP HÀNG TỰ ĐỘNG</h1>
        </div>
        <div style={{ fontSize: '2rem', fontWeight: 300, color: 'var(--accent-primary)' }}>
          {currentTime}
        </div>
      </header>

      {/* Main Content */}
      <div style={{ display: 'flex', flexDirection: 'column', flex: 1, padding: '0 2rem 2rem 2rem', gap: '2rem' }}>
        
        {/* Top: Recent Call Announcement */}
        <div className="glass-panel" style={{ flex: '0 0 auto', height: '400px', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '3rem', position: 'relative', overflow: 'hidden' }}>
          {recentCall ? (
            <>
              <div style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, background: 'var(--accent-gradient)', opacity: 0.1, animation: 'pulse 2s infinite' }} />
              <Volume2 size={48} color="var(--accent-primary)" style={{ animation: 'bounce 1s infinite', marginBottom: '1rem' }} />
              <h2 style={{ fontSize: '2rem', color: 'var(--text-secondary)' }}>XIN MỜI KHÁCH HÀNG SỐ</h2>
              <div style={{ fontSize: '6rem', fontWeight: 800, color: 'var(--accent-primary)', lineHeight: 1, margin: '1rem 0', textShadow: 'var(--shadow-glow)' }}>
                {recentCall.ticketNumber}
              </div>
              <h2 style={{ fontSize: '2.5rem', color: 'white' }}>ĐẾN QUẦY <span className="text-gradient">{recentCall.counterName}</span></h2>
              <p style={{ fontSize: '1.25rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>Dịch vụ: {recentCall.serviceName}</p>
            </>
          ) : (
            <div style={{ textAlign: 'center', opacity: 0.5 }}>
              <Monitor size={64} style={{ marginBottom: '1.5rem', display: 'inline-block' }} />
              <h2 style={{ fontSize: '2rem' }}>Chưa có phiên gọi số nào...</h2>
            </div>
          )}
        </div>

        {/* Bottom: List of Waiting Numbers */}
        <div className="glass-panel" style={{ flex: 1, padding: '2rem', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <h3 style={{ fontSize: '1.5rem', color: 'var(--text-secondary)', marginBottom: '1.5rem', borderBottom: '1px solid var(--border-light)', paddingBottom: '1rem' }}>
            DANH SÁCH SỐ ĐANG CHỜ PHỤC VỤ ({allWaitingNumbers.length})
          </h3>
          
          <div style={{ flex: 1, overflowY: 'auto', paddingRight: '1rem' }}>
            {allWaitingNumbers.length > 0 ? (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: '1.5rem' }}>
                {allWaitingNumbers.map((ticket, idx) => (
                  <div key={idx} style={{ 
                    background: 'var(--bg-secondary)', 
                    padding: '1.5rem', 
                    borderRadius: 'var(--radius-lg)', 
                    border: '1px solid var(--border-light)',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)'
                  }}>
                    <span style={{ fontSize: '2.5rem', fontWeight: 800, color: 'white', lineHeight: 1 }}>{ticket.ticketNumber}</span>
                  </div>
                ))}
              </div>
            ) : (
              <div style={{ textAlign: 'center', color: 'var(--text-secondary)', marginTop: '3rem', fontSize: '1.2rem' }}>
                Hiện không có khách hàng nào đang chờ.
              </div>
            )}
          </div>
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
