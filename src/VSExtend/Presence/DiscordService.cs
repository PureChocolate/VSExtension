using System;
using System.Threading;
using DiscordRPC;
using VSExtend.Git;
using VSExtend.Options;

namespace VSExtend.Presence
{
	public sealed class DiscordService : IDisposable
	{
		private readonly object _gate = new object();
		private readonly Action<string> _log;

		private DiscordRpcClient _client;
		private Timer _debounce;
		private Timer _reconnect;
		private bool _disposed;

		private bool _enabled;
		private string _appId = "";
		private DateTimeOffset _sessionStart = DateTimeOffset.UtcNow;

		private PresenceContext _ctx = PresenceContext.Empty;
		private GitInfo _git = GitInfo.Empty;
		private OptionsSnapshot _options = new OptionsSnapshot();
		private RichPresence _lastPresence;

		public DiscordService(Action<string> log)
		{
			_log = log ?? (_ => { });
			_debounce = new Timer(Flush, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			_reconnect = new Timer(OnReconnect, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		}

		public void Configure(bool enabled, string appId)
		{
			lock (_gate)
			{
				_enabled = enabled;
				_appId = (appId ?? "").Trim();
				if (_enabled && _appId.Length == 0)
				{
					_log("VSExtend: enabled but no Application ID configured (Tools > Options > VSExtend).");
					_enabled = false;
				}
				if (_enabled)
					_sessionStart = DateTimeOffset.UtcNow;
			}
			if (!_enabled) Teardown(true);
			else ScheduleFlush(0);
		}

		public void Push(PresenceContext ctx, GitInfo git, OptionsSnapshot options)
		{
			lock (_gate)
			{
				_ctx = ctx ?? PresenceContext.Empty;
				_git = git ?? GitInfo.Empty;
				_options = options ?? new OptionsSnapshot();
			}
			ScheduleFlush(700);
		}

		private void ScheduleFlush(int dueMs)
		{
			try
			{
				_debounce.Change(TimeSpan.FromMilliseconds(dueMs), Timeout.InfiniteTimeSpan);
			}
			catch (ObjectDisposedException) { }
		}

		private void Flush(object state)
		{
			RichPresence presence;
			lock (_gate)
			{
				if (_disposed || !_enabled) return;
				if (!EnsureConnected()) return;
				presence = PresenceBuilder.Build(_ctx, _git, _options, _sessionStart);
				_lastPresence = presence;
			}
			SafeSend(presence);
		}

		private bool EnsureConnected()
		{
			if (_client != null && _client.IsInitialized) return true;
			if (_client != null)
			{
				try { _client.Deinitialize(); } catch { }
				try { _client.Dispose(); } catch { }
				_client = null;
			}
			try
			{
				var client = new DiscordRpcClient(_appId);
				client.OnReady += (s, e) => { if (_client == client) _log("VSExtend: connected to Discord."); };
				client.OnClose += (s, e) =>
				{
					_log("VSExtend: Discord closed the connection (" + (e?.Reason ?? "unknown") + ").");
					ScheduleReconnect();
				};
				client.OnError += (s, e) =>
				{
					_log("VSExtend: Discord RPC error: " + (e?.Code ?? 0) + " " + (e?.Message ?? ""));
					ScheduleReconnect();
				};
				if (!client.Initialize())
				{
					_log("VSExtend: could not connect to Discord (is it running, and is the Application ID valid?).");
					client.Dispose();
					_client = null;
					return false;
				}
				_client = client;
				return true;
			}
			catch (Exception ex)
			{
				_log("VSExtend: failed to start Discord RPC: " + ex.Message);
				return false;
			}
		}

		private void ScheduleReconnect()
		{
			try
			{
				lock (_gate)
				{
					if (_disposed) return;
					_client = null;
				}
				_reconnect.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
			}
			catch (ObjectDisposedException) { }
		}

		private void OnReconnect(object state)
		{
			lock (_gate)
			{
				if (_disposed || !_enabled) return;
			}
			Flush(null);
		}

		private void SafeSend(RichPresence presence)
		{
			DiscordRpcClient client;
			lock (_gate) { client = _client; }
			if (client == null) return;
			try
			{
				client.SetPresence(presence);
			}
			catch (Exception ex)
			{
				_log("VSExtend: failed to set presence: " + ex.Message);
			}
		}

		private void Teardown(bool clearPresence)
		{
			DiscordRpcClient client;
			lock (_gate)
			{
				client = _client;
				_client = null;
				_lastPresence = null;
			}
			if (client != null)
			{
				try
				{
					if (clearPresence)
						client.ClearPresence();
					client.Deinitialize();
				}
				catch { }
				try { client.Dispose(); } catch { }
			}
		}

		public void Dispose()
		{
			lock (_gate)
			{
				if (_disposed) return;
				_disposed = true;
			}
			try { _debounce.Dispose(); } catch { }
			try { _reconnect.Dispose(); } catch { }
			Teardown(true);
		}
	}
}
