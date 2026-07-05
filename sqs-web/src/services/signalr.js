import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const HUB_URL = 'http://localhost:5000/hubs/queue';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.listeners = new Map();
  }

  async connect() {
    if (this.isConnected) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected!', connectionId);
    });

    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
      this.isConnected = false;
    });

    try {
      await this.connection.start();
      this.isConnected = true;
      console.log('SignalR Connected!');
    } catch (err) {
      console.error('SignalR Connection Error: ', err);
      // Retry connection after 5 seconds
      setTimeout(() => this.connect(), 5000);
    }
  }

  async joinGroup(groupName) {
    if (!this.isConnected) await this.connect();
    try {
      await this.connection.invoke('JoinGroup', groupName);
      console.log(`Joined SignalR group: ${groupName}`);
    } catch (err) {
      console.error(`Error joining group ${groupName}:`, err);
    }
  }

  async leaveGroup(groupName) {
    if (!this.isConnected) return;
    try {
      await this.connection.invoke('LeaveGroup', groupName);
      console.log(`Left SignalR group: ${groupName}`);
    } catch (err) {
      console.error(`Error leaving group ${groupName}:`, err);
    }
  }

  on(eventName, callback) {
    if (!this.connection) return;
    if (!this.listeners.has(eventName)) {
      this.listeners.set(eventName, []);
      this.connection.on(eventName, (data) => {
        this.listeners.get(eventName).forEach((cb) => cb(data));
      });
    }
    this.listeners.get(eventName).push(callback);
  }

  off(eventName, callback) {
    if (!this.connection) return;
    if (this.listeners.has(eventName)) {
      const callbacks = this.listeners.get(eventName);
      const filtered = callbacks.filter((cb) => cb !== callback);
      if (filtered.length === 0) {
        this.connection.off(eventName);
        this.listeners.delete(eventName);
      } else {
        this.listeners.set(eventName, filtered);
      }
    }
  }
}

const signalRService = new SignalRService();
export default signalRService;
