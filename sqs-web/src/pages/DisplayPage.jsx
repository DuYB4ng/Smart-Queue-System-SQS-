import React, { useState, useEffect } from 'react';
import api from '../services/api';
import signalRService from '../services/signalr';
import { Volume2, Monitor } from 'lucide-react';

const DisplayPage = () => {
  const [callingTickets, setCallingTickets] = useState([]);
  const [recentCall, setRecentCall] = useState(null);
  const [currentTime, setCurrentTime] = useState(new Date().toLocaleTimeString('vi-VN'));

  useEffect(() => {
    loadCallingTickets();
    signalRService.connect().then(() => {
      signalRService.joinGroup('display');
    });

    const timer = setInterval(() => {
      setCurrentTime(new Date().toLocaleTimeString('vi-VN'));
    }, 1000);

    signalRService.on('TicketCalled', (payload) => {
      setRecentCall(payload);
      playDingSound(payload);
      
      // Clear recent call highlight after 10s
      setTimeout(() => {
        setRecentCall(prev => prev?.ticketId === payload.ticketId ? null : prev);
      }, 10000);
      
      // Update the calling list
      loadCallingTickets();
    });

    // Force load voices for Text-to-Speech
    if ('speechSynthesis' in window) {
      window.speechSynthesis.getVoices();
      window.speechSynthesis.onvoiceschanged = () => {
        const v = window.speechSynthesis.getVoices();
        console.log('Voices loaded:', v.length);
      };
    }

    return () => {
      signalRService.leaveGroup('display');
      signalRService.off('TicketCalled');
      clearInterval(timer);
    };
  }, []);

  const loadCallingTickets = async () => {
    try {
      const res = await api.get('/tickets/calling');
      setCallingTickets(res.data);
    } catch (err) {
      console.error('Error loading calling tickets', err);
    }
  };

  const playDingSound = (payload) => {
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

    // Text to Speech
    if (payload && 'speechSynthesis' in window) {
      setTimeout(() => {
        const cName = payload.counterName || "Quầy";
        const text = `Xin mời khách hàng số ${payload.ticketNumber}, đến ${cName.toLowerCase()}`;
        const msg = new SpeechSynthesisUtterance(text);
        msg.lang = 'vi-VN';
        
        // Cố gắng tìm đích danh giọng đọc tiếng Việt (Google tiếng Việt, Microsoft An, v.v.)
        const voices = window.speechSynthesis.getVoices();
        const viVoice = voices.find(voice => 
          voice.lang.includes('vi') || 
          voice.lang.includes('VI') || 
          voice.name.includes('Vietnamese') ||
          voice.name.includes('Google tiếng Việt') ||
          voice.name.includes('An')
        );
        
        if (viVoice) {
          console.log("Found Vietnamese voice:", viVoice.name);
          msg.voice = viVoice;
        } else {
          console.log("No Vietnamese voice found. Available voices:", voices.map(v => v.name));
        }
        
        msg.rate = 0.85; // Slightly slower for clarity
        msg.pitch = 1;
        window.speechSynthesis.speak(msg);
      }, 1000); // Wait 1 second for the ding to finish
    }
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: 'var(--bg-primary)' }}>
      {/* Header */}
      <header className="glass-panel" style={{ padding: '1.5rem 3rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '2rem', borderRadius: 'var(--radius-lg)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <Monitor size={32} color="var(--accent-primary)" />
          <h1 style={{ fontSize: '2rem', fontWeight: 700, margin: 0, color: 'var(--text-primary)' }}>HỆ THỐNG XẾP HÀNG TỰ ĐỘNG</h1>
        </div>
        <div style={{ fontSize: '2rem', fontWeight: 300, color: 'var(--accent-primary)' }}>
          {currentTime}
        </div>
      </header>

      {/* Main Content */}
      <div style={{ display: 'flex', flexDirection: 'row', flex: 1, padding: '0 2rem 2rem 2rem', gap: '2rem' }}>
        
        {/* Left: Recent Call Announcement */}
        <div className="glass-panel" style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '3rem', position: 'relative', overflow: 'hidden' }}>
          {recentCall ? (
            <>
              <div style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, background: 'var(--accent-gradient)', opacity: 0.1, animation: 'pulse 2s infinite' }} />
              <Volume2 size={64} color="var(--accent-primary)" style={{ animation: 'bounce 1s infinite', marginBottom: '2rem' }} />
              <h2 style={{ fontSize: '2.5rem', color: 'var(--text-secondary)' }}>XIN MỜI KHÁCH HÀNG SỐ</h2>
              <div style={{ fontSize: '8rem', fontWeight: 800, color: 'var(--accent-primary)', lineHeight: 1, margin: '2rem 0', textShadow: 'var(--shadow-glow)' }}>
                {recentCall.ticketNumber}
              </div>
              <h2 style={{ fontSize: '3.5rem', color: 'var(--text-primary)' }}>ĐẾN QUẦY <span className="text-gradient">{recentCall.counterName}</span></h2>
              <p style={{ fontSize: '1.5rem', color: 'var(--text-secondary)', marginTop: '1rem' }}>Dịch vụ: {recentCall.serviceName}</p>
            </>
          ) : (
            <div style={{ textAlign: 'center', opacity: 0.5 }}>
              <Monitor size={80} style={{ marginBottom: '2rem', display: 'inline-block', color: 'var(--text-primary)' }} />
              <h2 style={{ fontSize: '2.5rem', color: 'var(--text-primary)' }}>Chưa có phiên gọi số nào...</h2>
            </div>
          )}
        </div>

        {/* Right: Table of currently calling numbers */}
        <div className="glass-panel" style={{ flex: 1, padding: '2rem', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <h3 style={{ fontSize: '1.8rem', color: 'var(--text-primary)', marginBottom: '1.5rem', borderBottom: '2px solid var(--border-light)', paddingBottom: '1rem', textAlign: 'center' }}>
            DANH SÁCH ĐANG ĐƯỢC GỌI
          </h3>
          
          <div style={{ flex: 1, overflowY: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'center' }}>
              <thead>
                <tr>
                  <th style={{ padding: '1.5rem', fontSize: '1.5rem', color: 'var(--text-secondary)', borderBottom: '1px solid var(--border-light)' }}>Số Thứ Tự</th>
                  <th style={{ padding: '1.5rem', fontSize: '1.5rem', color: 'var(--text-secondary)', borderBottom: '1px solid var(--border-light)' }}>Quầy Phục Vụ</th>
                </tr>
              </thead>
              <tbody>
                {callingTickets.length > 0 ? (
                  callingTickets.map((ticket, idx) => (
                    <tr key={ticket.ticketId} style={{ 
                      background: idx % 2 === 0 ? 'rgba(0,0,0,0.02)' : 'transparent',
                      borderBottom: '1px solid var(--border-light)',
                      transition: 'all 0.3s'
                    }}>
                      <td style={{ padding: '2rem', fontSize: '3rem', fontWeight: 700, color: 'var(--accent-primary)' }}>
                        {ticket.ticketNumber}
                      </td>
                      <td style={{ padding: '2rem', fontSize: '2.5rem', fontWeight: 600, color: 'var(--text-primary)' }}>
                        {ticket.counterName}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={2} style={{ padding: '3rem', fontSize: '1.5rem', color: 'var(--text-secondary)' }}>
                      Không có số nào đang được gọi.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
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
          50% { transform: translateY(-15px); }
        }
      `}</style>
    </div>
  );
};

export default DisplayPage;
