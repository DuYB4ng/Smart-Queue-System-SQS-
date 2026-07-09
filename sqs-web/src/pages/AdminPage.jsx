import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import Navbar from '../components/Navbar';
import {
  BarChart3, Users, Activity, CheckCircle2, XCircle,
  RefreshCw, Calendar, Search, Edit3, Save, X,
  TrendingUp, Clock, Award, Settings, List, ChevronLeft, ChevronRight,
  Plus, Trash2, ToggleLeft, ToggleRight
} from 'lucide-react';

/* ─────────────────────────────────────────────────────── */
/*  Tabs                                                   */
/* ─────────────────────────────────────────────────────── */
const TABS = [
  { id: 'dashboard', label: 'Tổng quan',       icon: BarChart3 },
  { id: 'staff',     label: 'Nhân viên',        icon: Users     },
  { id: 'tickets',   label: 'Lịch sử vé',       icon: List      },
  { id: 'services',  label: 'Dịch vụ',          icon: Settings  },
];

/* ─────────────────────────────────────────────────────── */
/*  Helpers                                                */
/* ─────────────────────────────────────────────────────── */
const fmtTime = (iso) => {
  if (!iso) return '—';
  return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
};
const fmtDate = (iso) => {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('vi-VN');
};

const STATUS_MAP = {
  Waiting:   { label: 'Đang chờ',   color: '#f59e0b' },
  Calling:   { label: 'Đang gọi',   color: '#3b82f6' },
  Completed: { label: 'Hoàn thành', color: '#10b981' },
  Canceled:  { label: 'Đã hủy',     color: '#ef4444' },
};

/* ─────────────────────────────────────────────────────── */
/*  StatCard                                               */
/* ─────────────────────────────────────────────────────── */
const StatCard = ({ icon: Icon, title, value, sub, color }) => (
  <div style={{
    background: 'var(--bg-card)',
    border: `1px solid var(--border-light)`,
    borderTop: `4px solid ${color}`,
    borderRadius: 'var(--radius-lg)',
    padding: '1.5rem',
    display: 'flex', flexDirection: 'column', gap: '0.5rem',
  }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)' }}>
      <Icon size={18} color={color} />
      <span style={{ fontSize: '0.85rem', fontWeight: 500 }}>{title}</span>
    </div>
    <div style={{ fontSize: '2.8rem', fontWeight: 800, lineHeight: 1, color: 'var(--text-primary)' }}>{value}</div>
    {sub && <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>{sub}</div>}
  </div>
);

/* ─────────────────────────────────────────────────────── */
/*  DashboardTab                                           */
/* ─────────────────────────────────────────────────────── */
const DashboardTab = () => {
  const [data,    setData]    = useState(null);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState(null);

  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const res = await api.get('/admin/dashboard');
      setData(res.data);
    } catch (e) {
      setError('Không tải được dữ liệu. Vui lòng thử lại.');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Đang tải...</div>;
  if (error)   return (
    <div style={{ padding: '3rem', textAlign: 'center' }}>
      <p style={{ color: 'var(--danger)', marginBottom: '1rem' }}>{error}</p>
      <button onClick={load} style={{ padding: '0.5rem 1.5rem', background: 'var(--accent-gradient)', color: '#fff', borderRadius: 'var(--radius-full)', fontWeight: 600 }}>
        Thử lại
      </button>
    </div>
  );

  const s = data?.summary || {};

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
      {/* KPI cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1.25rem' }}>
        <StatCard icon={Users}       title="Đang chờ"      value={s.waiting || 0}   color="#f59e0b" />
        <StatCard icon={Activity}    title="Đang phục vụ"  value={s.calling || 0}   color="#3b82f6" />
        <StatCard icon={CheckCircle2} title="Đã hoàn thành" value={s.completed || 0} color="#10b981" sub={`Tỉ lệ: ${s.completionRate ?? 0}%`} />
        <StatCard icon={XCircle}     title="Đã hủy / Bỏ"  value={s.canceled || 0}  color="#ef4444" />
        <StatCard icon={Calendar}    title="Đặt trước hôm nay" value={s.appointments || 0} color="#8b5cf6" />
        <StatCard icon={TrendingUp}  title="Tổng hôm nay"  value={s.total || 0}     color="#06b6d4" />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
        {/* Top Staff */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', padding: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.25rem' }}>
            <Award size={20} color="var(--accent-primary)" />
            <h3 style={{ fontWeight: 700 }}>BXH Nhân viên hôm nay</h3>
          </div>
          {data?.topStaff?.length > 0 ? data.topStaff.map((st, i) => (
            <div key={st.staffId} style={{
              display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              padding: '0.75rem 1rem', marginBottom: '0.5rem',
              background: i === 0 ? 'rgba(245,158,11,0.08)' : 'rgba(0,0,0,0.04)',
              borderRadius: 'var(--radius-md)',
              borderLeft: `3px solid ${i === 0 ? '#f59e0b' : i === 1 ? '#94a3b8' : i === 2 ? '#b45309' : 'transparent'}`
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                <div style={{
                  width: 28, height: 28, borderRadius: '50%',
                  background: i === 0 ? '#f59e0b' : 'var(--bg-secondary)',
                  color: i === 0 ? '#fff' : 'var(--text-secondary)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: 700, fontSize: '0.85rem'
                }}>{i + 1}</div>
                <div>
                  <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>{st.name}</div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{st.position || 'Nhân viên'}</div>
                </div>
              </div>
              <div style={{ fontWeight: 700, color: '#10b981' }}>{st.todayKpi} <span style={{ fontWeight: 400, fontSize: '0.8rem', color: 'var(--text-secondary)' }}>vé</span></div>
            </div>
          )) : <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '1.5rem' }}>Chưa có dữ liệu hôm nay</p>}
        </div>

        {/* By Service */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', padding: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.25rem' }}>
            <BarChart3 size={20} color="var(--accent-primary)" />
            <h3 style={{ fontWeight: 700 }}>Thống kê theo dịch vụ</h3>
          </div>
          {data?.byService?.length > 0 ? data.byService.map(sv => {
            const pct = sv.total > 0 ? Math.round(sv.completed / sv.total * 100) : 0;
            return (
              <div key={sv.serviceId} style={{ marginBottom: '1rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.3rem' }}>
                  <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>{sv.serviceName}</span>
                  <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>{sv.completed}/{sv.total}</span>
                </div>
                <div style={{ height: 8, background: 'rgba(0,0,0,0.08)', borderRadius: 999 }}>
                  <div style={{ height: '100%', width: `${pct}%`, background: 'var(--accent-gradient)', borderRadius: 999, transition: 'width 0.5s ease' }} />
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                  Chờ: {sv.waiting} · Gọi: {sv.calling} · Hủy: {sv.canceled}
                </div>
              </div>
            );
          }) : <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '1.5rem' }}>Chưa có dữ liệu</p>}
        </div>
      </div>
    </div>
  );
};

/* ─────────────────────────────────────────────────────── */
/*  StaffTab                                               */
/* ─────────────────────────────────────────────────────── */
const StaffTab = () => {
  const [list,    setList]    = useState([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(null); // staffId being edited
  const [editVal, setEditVal] = useState({});
  const [saving,  setSaving]  = useState(false);
  const [msg,     setMsg]     = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    try { const r = await api.get('/admin/staff'); setList(r.data); }
    catch { setMsg({ type: 'error', text: 'Không tải được danh sách nhân viên.' }); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const startEdit = (s) => {
    setEditing(s.staffId);
    setEditVal({ position: s.position || '', counterId: s.counterId || '', isActive: s.isActive });
  };

  const cancelEdit = () => { setEditing(null); setEditVal({}); };

  const saveEdit = async (staffId) => {
    setSaving(true);
    try {
      await api.put(`/admin/staff/${staffId}`, {
        position:  editVal.position  || null,
        counterId: editVal.counterId ? parseInt(editVal.counterId) : null,
        isActive:  editVal.isActive,
      });
      setMsg({ type: 'success', text: 'Cập nhật thành công!' });
      setEditing(null);
      load();
    } catch (e) {
      setMsg({ type: 'error', text: e.response?.data?.message || 'Lỗi khi cập nhật.' });
    } finally { setSaving(false); }
  };

  return (
    <div>
      {msg && (
        <div style={{ padding: '0.75rem 1rem', marginBottom: '1rem', borderRadius: 'var(--radius-md)',
          background: msg.type === 'success' ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)',
          color: msg.type === 'success' ? '#10b981' : '#ef4444',
          border: `1px solid ${msg.type === 'success' ? '#10b981' : '#ef4444'}40`
        }}>
          {msg.text}
        </div>
      )}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', overflow: 'hidden' }}>
        <div style={{ padding: '1rem 1.5rem', borderBottom: '1px solid var(--border-light)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3 style={{ fontWeight: 700 }}>Danh sách nhân viên ({list.length})</h3>
          <button onClick={load} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', padding: '0.4rem 1rem', background: 'transparent', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-full)', color: 'var(--text-secondary)', cursor: 'pointer', fontSize: '0.85rem' }}>
            <RefreshCw size={14} /> Làm mới
          </button>
        </div>

        {loading ? <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Đang tải...</div> : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: 'rgba(0,0,0,0.03)', fontSize: '0.82rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                {['Nhân viên', 'Email', 'Chức vụ', 'Quầy', 'KPI hôm nay', 'Tổng KPI', 'Trạng thái', ''].map(h => (
                  <th key={h} style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {list.map(s => (
                <tr key={s.staffId} style={{ borderTop: '1px solid var(--border-light)', transition: 'background 0.15s' }}
                  onMouseEnter={e => e.currentTarget.style.background = 'rgba(0,0,0,0.02)'}
                  onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                  <td style={{ padding: '0.9rem 1rem', fontWeight: 600 }}>{s.name}</td>
                  <td style={{ padding: '0.9rem 1rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>{s.email}</td>
                  <td style={{ padding: '0.9rem 1rem' }}>
                    {editing === s.staffId
                      ? <input value={editVal.position} onChange={e => setEditVal(v => ({ ...v, position: e.target.value }))}
                          style={{ padding: '0.3rem 0.6rem', border: '1px solid var(--border-light)', borderRadius: 6, width: 120, fontSize: '0.85rem' }} />
                      : <span style={{ fontSize: '0.88rem' }}>{s.position || '—'}</span>}
                  </td>
                  <td style={{ padding: '0.9rem 1rem' }}>
                    {editing === s.staffId
                      ? <input type="number" value={editVal.counterId} onChange={e => setEditVal(v => ({ ...v, counterId: e.target.value }))}
                          style={{ padding: '0.3rem 0.6rem', border: '1px solid var(--border-light)', borderRadius: 6, width: 70, fontSize: '0.85rem' }} />
                      : <span style={{ fontWeight: 600 }}>{s.counterId ? `Quầy ${s.counterId}` : '—'}</span>}
                  </td>
                  <td style={{ padding: '0.9rem 1rem', fontWeight: 700, color: '#10b981' }}>{s.todayKpi}</td>
                  <td style={{ padding: '0.9rem 1rem' }}>{s.totalKpi}</td>
                  <td style={{ padding: '0.9rem 1rem' }}>
                    {editing === s.staffId
                      ? <button onClick={() => setEditVal(v => ({ ...v, isActive: !v.isActive }))}
                          style={{ background: 'none', border: 'none', cursor: 'pointer', color: editVal.isActive ? '#10b981' : '#ef4444', display: 'flex', alignItems: 'center', gap: 4 }}>
                          {editVal.isActive ? <ToggleRight size={22} /> : <ToggleLeft size={22} />}
                          <span style={{ fontSize: '0.8rem' }}>{editVal.isActive ? 'Đang HĐ' : 'Nghỉ'}</span>
                        </button>
                      : <span style={{
                          display: 'inline-block', padding: '0.2rem 0.7rem', borderRadius: 999, fontSize: '0.78rem', fontWeight: 600,
                          background: s.isActive ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)',
                          color: s.isActive ? '#10b981' : '#ef4444'
                        }}>{s.isActive ? 'Đang HĐ' : 'Nghỉ'}</span>}
                  </td>
                  <td style={{ padding: '0.9rem 1rem' }}>
                    {editing === s.staffId ? (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button onClick={() => saveEdit(s.staffId)} disabled={saving}
                          style={{ padding: '0.3rem 0.8rem', background: '#10b981', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.82rem' }}>
                          <Save size={13} /> Lưu
                        </button>
                        <button onClick={cancelEdit}
                          style={{ padding: '0.3rem 0.8rem', background: 'transparent', border: '1px solid var(--border-light)', borderRadius: 6, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.82rem' }}>
                          <X size={13} /> Huỷ
                        </button>
                      </div>
                    ) : (
                      <button onClick={() => startEdit(s)}
                        style={{ padding: '0.3rem 0.8rem', background: 'transparent', border: '1px solid var(--border-light)', borderRadius: 6, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
                        <Edit3 size={13} /> Sửa
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

/* ─────────────────────────────────────────────────────── */
/*  TicketsTab                                             */
/* ─────────────────────────────────────────────────────── */
const TicketsTab = () => {
  const today = new Date().toISOString().split('T')[0];
  const [date,    setDate]    = useState(today);
  const [status,  setStatus]  = useState('');
  const [page,    setPage]    = useState(1);
  const [data,    setData]    = useState({ data: [], total: 0, pages: 1 });
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const params = { date, page, limit: 15 };
      if (status) params.status = status;
      const r = await api.get('/admin/tickets', { params });
      setData(r.data);
    } catch { }
    finally { setLoading(false); }
  }, [date, status, page]);

  useEffect(() => { setPage(1); }, [date, status]);
  useEffect(() => { load(); }, [load]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      {/* Filters */}
      <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center', background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', padding: '1rem 1.5rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Calendar size={16} color="var(--text-secondary)" />
          <input type="date" value={date} onChange={e => setDate(e.target.value)}
            style={{ padding: '0.4rem 0.75rem', border: '1px solid var(--border-light)', borderRadius: 8, fontSize: '0.9rem' }} />
        </div>
        <select value={status} onChange={e => setStatus(e.target.value)}
          style={{ padding: '0.4rem 0.75rem', border: '1px solid var(--border-light)', borderRadius: 8, fontSize: '0.9rem' }}>
          <option value="">Tất cả trạng thái</option>
          <option value="Waiting">Đang chờ</option>
          <option value="Calling">Đang gọi</option>
          <option value="Completed">Hoàn thành</option>
          <option value="Canceled">Đã hủy</option>
        </select>
        <span style={{ marginLeft: 'auto', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
          Tổng: <strong>{data.total}</strong> vé
        </span>
      </div>

      {/* Table */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', overflow: 'hidden' }}>
        {loading ? <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Đang tải...</div> : (
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.88rem' }}>
            <thead>
              <tr style={{ background: 'rgba(0,0,0,0.03)', color: 'var(--text-secondary)', fontSize: '0.78rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                {['Số vé', 'Khách hàng', 'Dịch vụ', 'Quầy', 'Nhân viên', 'Giờ tạo', 'Giờ gọi', 'Trạng thái'].map(h => (
                  <th key={h} style={{ padding: '0.7rem 1rem', textAlign: 'left', fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.data.length === 0
                ? <tr><td colSpan={8} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Không có dữ liệu</td></tr>
                : data.data.map(t => {
                  const st = STATUS_MAP[t.status] || { label: t.status, color: '#94a3b8' };
                  return (
                    <tr key={t.id} style={{ borderTop: '1px solid var(--border-light)' }}>
                      <td style={{ padding: '0.7rem 1rem', fontWeight: 700, fontFamily: 'monospace', fontSize: '1rem' }}>{t.ticketNumber}</td>
                      <td style={{ padding: '0.7rem 1rem' }}>{t.customerName || '—'}</td>
                      <td style={{ padding: '0.7rem 1rem', color: 'var(--text-secondary)' }}>{t.serviceName}</td>
                      <td style={{ padding: '0.7rem 1rem' }}>{t.counterName || '—'}</td>
                      <td style={{ padding: '0.7rem 1rem' }}>{t.staffName || '—'}</td>
                      <td style={{ padding: '0.7rem 1rem', color: 'var(--text-secondary)' }}>{fmtTime(t.createdAt)}</td>
                      <td style={{ padding: '0.7rem 1rem', color: 'var(--text-secondary)' }}>{fmtTime(t.calledAt)}</td>
                      <td style={{ padding: '0.7rem 1rem' }}>
                        <span style={{ padding: '0.2rem 0.65rem', borderRadius: 999, fontSize: '0.75rem', fontWeight: 600, background: `${st.color}18`, color: st.color }}>
                          {st.label}
                        </span>
                      </td>
                    </tr>
                  );
                })}
            </tbody>
          </table>
        )}

        {/* Pagination */}
        {data.pages > 1 && (
          <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '1rem', padding: '1rem', borderTop: '1px solid var(--border-light)' }}>
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
              style={{ display: 'flex', alignItems: 'center', gap: 4, padding: '0.4rem 0.8rem', border: '1px solid var(--border-light)', borderRadius: 8, background: 'transparent', cursor: page === 1 ? 'not-allowed' : 'pointer', opacity: page === 1 ? 0.4 : 1 }}>
              <ChevronLeft size={16} /> Trước
            </button>
            <span style={{ fontSize: '0.88rem', color: 'var(--text-secondary)' }}>Trang {page} / {data.pages}</span>
            <button onClick={() => setPage(p => Math.min(data.pages, p + 1))} disabled={page === data.pages}
              style={{ display: 'flex', alignItems: 'center', gap: 4, padding: '0.4rem 0.8rem', border: '1px solid var(--border-light)', borderRadius: 8, background: 'transparent', cursor: page === data.pages ? 'not-allowed' : 'pointer', opacity: page === data.pages ? 0.4 : 1 }}>
              Sau <ChevronRight size={16} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

/* ─────────────────────────────────────────────────────── */
/*  ServicesTab                                            */
/* ─────────────────────────────────────────────────────── */
const ServicesTab = () => {
  const [list,    setList]    = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg,     setMsg]     = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [form,    setForm]    = useState({ name: '', code: '', description: '' });
  const [saving,  setSaving]  = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try { const r = await api.get('/services'); setList(r.data); }
    catch { setMsg({ type: 'error', text: 'Không tải được dịch vụ.' }); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const create = async () => {
    if (!form.name || !form.code) return setMsg({ type: 'error', text: 'Vui lòng nhập đủ tên và mã dịch vụ.' });
    setSaving(true);
    try {
      await api.post('/admin/services', form);
      setMsg({ type: 'success', text: 'Tạo dịch vụ thành công!' });
      setShowForm(false); setForm({ name: '', code: '', description: '' });
      load();
    } catch (e) {
      setMsg({ type: 'error', text: e.response?.data?.message || 'Lỗi khi tạo dịch vụ.' });
    } finally { setSaving(false); }
  };

  const toggle = async (svc) => {
    try {
      await api.put(`/admin/services/${svc.id}`, { isActive: !svc.isActive });
      setMsg({ type: 'success', text: `Đã ${svc.isActive ? 'vô hiệu hóa' : 'kích hoạt'} dịch vụ.` });
      load();
    } catch { setMsg({ type: 'error', text: 'Lỗi khi cập nhật dịch vụ.' }); }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      {msg && (
        <div style={{ padding: '0.75rem 1rem', borderRadius: 'var(--radius-md)',
          background: msg.type === 'success' ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)',
          color: msg.type === 'success' ? '#10b981' : '#ef4444',
          border: `1px solid ${msg.type === 'success' ? '#10b981' : '#ef4444'}40`
        }}>
          {msg.text}
        </div>
      )}

      {/* Add new service form */}
      {showForm ? (
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--accent-primary)40', borderRadius: 'var(--radius-lg)', padding: '1.5rem' }}>
          <h4 style={{ fontWeight: 700, marginBottom: '1rem' }}>Thêm dịch vụ mới</h4>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div>
              <label style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '0.4rem' }}>Tên dịch vụ *</label>
              <input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="VD: Cấp phát tài liệu"
                style={{ width: '100%', padding: '0.5rem 0.75rem', border: '1px solid var(--border-light)', borderRadius: 8 }} />
            </div>
            <div>
              <label style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '0.4rem' }}>Mã dịch vụ *</label>
              <input value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value.toUpperCase() }))}
                placeholder="VD: DOC"
                style={{ width: '100%', padding: '0.5rem 0.75rem', border: '1px solid var(--border-light)', borderRadius: 8, fontFamily: 'monospace' }} />
            </div>
          </div>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '0.4rem' }}>Mô tả</label>
            <input value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              placeholder="Mô tả ngắn về dịch vụ..."
              style={{ width: '100%', padding: '0.5rem 0.75rem', border: '1px solid var(--border-light)', borderRadius: 8 }} />
          </div>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <button onClick={create} disabled={saving}
              style={{ padding: '0.5rem 1.5rem', background: 'var(--accent-gradient)', color: '#fff', border: 'none', borderRadius: 'var(--radius-full)', fontWeight: 600, cursor: 'pointer' }}>
              {saving ? 'Đang lưu...' : 'Tạo dịch vụ'}
            </button>
            <button onClick={() => setShowForm(false)}
              style={{ padding: '0.5rem 1.5rem', background: 'transparent', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-full)', cursor: 'pointer' }}>
              Hủy
            </button>
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <button onClick={() => setShowForm(true)}
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '0.6rem 1.25rem', background: 'var(--accent-gradient)', color: '#fff', border: 'none', borderRadius: 'var(--radius-full)', fontWeight: 600, cursor: 'pointer' }}>
            <Plus size={16} /> Thêm dịch vụ
          </button>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1rem' }}>
        {loading ? <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)', gridColumn: '1/-1' }}>Đang tải...</div>
          : list.map(svc => (
          <div key={svc.id} style={{
            background: 'var(--bg-card)', border: '1px solid var(--border-light)',
            borderRadius: 'var(--radius-lg)', padding: '1.25rem',
            opacity: svc.isActive ? 1 : 0.55,
            borderLeft: `4px solid ${svc.isActive ? 'var(--accent-primary)' : '#94a3b8'}`
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
              <div>
                <div style={{ fontWeight: 700, fontSize: '1rem' }}>{svc.name}</div>
                <code style={{ fontSize: '0.78rem', background: 'rgba(0,0,0,0.06)', padding: '0.1rem 0.4rem', borderRadius: 4, color: 'var(--accent-primary)' }}>{svc.code}</code>
              </div>
              <button onClick={() => toggle(svc)} title={svc.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                style={{ background: 'none', border: 'none', cursor: 'pointer', color: svc.isActive ? '#10b981' : '#94a3b8', padding: 4 }}>
                {svc.isActive ? <ToggleRight size={24} /> : <ToggleLeft size={24} />}
              </button>
            </div>
            {svc.description && <p style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>{svc.description}</p>}
            <div style={{ marginTop: '0.75rem', fontSize: '0.78rem', color: svc.isActive ? '#10b981' : '#94a3b8', fontWeight: 600 }}>
              {svc.isActive ? '● Đang hoạt động' : '○ Đã vô hiệu hóa'}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

/* ─────────────────────────────────────────────────────── */
/*  AdminPage (main)                                       */
/* ─────────────────────────────────────────────────────── */
const AdminPage = ({ user, onLogout }) => {
  const navigate  = useNavigate();
  const [activeTab, setActiveTab] = useState('dashboard');

  useEffect(() => {
    if (!user || user.role !== 'Admin') navigate('/login');
  }, [user]);

  if (!user || user.role !== 'Admin') return null;

  const renderTab = () => {
    switch (activeTab) {
      case 'dashboard': return <DashboardTab />;
      case 'staff':     return <StaffTab />;
      case 'tickets':   return <TicketsTab />;
      case 'services':  return <ServicesTab />;
      default:          return null;
    }
  };

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-primary)' }}>
      <div style={{ maxWidth: 1280, margin: '0 auto', padding: '1.5rem 2rem' }}>
        <Navbar title="Admin Dashboard" user={user} onLogout={onLogout} />

        {/* Tab bar */}
        <div style={{ display: 'flex', gap: '0.25rem', marginBottom: '1.75rem', background: 'var(--bg-card)', border: '1px solid var(--border-light)', borderRadius: 'var(--radius-lg)', padding: '0.4rem', width: 'fit-content' }}>
          {TABS.map(tab => {
            const Icon = tab.icon;
            const active = activeTab === tab.id;
            return (
              <button key={tab.id} onClick={() => setActiveTab(tab.id)}
                style={{
                  display: 'flex', alignItems: 'center', gap: '0.5rem',
                  padding: '0.6rem 1.25rem', border: 'none', borderRadius: 'var(--radius-md)',
                  background: active ? 'var(--accent-gradient)' : 'transparent',
                  color: active ? '#fff' : 'var(--text-secondary)',
                  fontWeight: active ? 700 : 500, cursor: 'pointer',
                  fontSize: '0.88rem', transition: 'all 0.2s ease',
                }}>
                <Icon size={16} />
                {tab.label}
              </button>
            );
          })}
        </div>

        {/* Tab content */}
        {renderTab()}
      </div>
    </div>
  );
};

export default AdminPage;
