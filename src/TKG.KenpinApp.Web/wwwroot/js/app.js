/* ========================================
   TKG検品アプリ — 共通JavaScript
   ======================================== */

// APIベースURL（同一オリジン）
const API_BASE = '';

// --- セッション管理 ---

/** セッショントークンをCookieに保存 */
function setSessionToken(token) {
    document.cookie = `sessionToken=${encodeURIComponent(token)}; path=/; max-age=${8 * 3600}; SameSite=Strict`;
}

/** Cookieからセッショントークンを取得 */
function getSessionToken() {
    const match = document.cookie.match(/(?:^|;\s*)sessionToken=([^;]*)/);
    return match ? decodeURIComponent(match[1]) : null;
}

/** セッショントークンを削除 */
function clearSessionToken() {
    document.cookie = 'sessionToken=; path=/; max-age=0';
}

/** ログインユーザー情報をsessionStorageに保存 */
function setUserInfo(info) {
    sessionStorage.setItem('userInfo', JSON.stringify(info));
}

/** ログインユーザー情報を取得 */
function getUserInfo() {
    const data = sessionStorage.getItem('userInfo');
    return data ? JSON.parse(data) : null;
}

/** ユーザー情報をクリア */
function clearUserInfo() {
    sessionStorage.removeItem('userInfo');
}

// --- API呼び出し ---

/**
 * API呼び出しヘルパー
 * @param {string} method HTTPメソッド
 * @param {string} url APIパス
 * @param {object|null} body リクエストボディ
 * @returns {Promise<object|null>} レスポンスデータ
 */
async function apiCall(method, url, body = null) {
    const opts = {
        method,
        headers: {
            'Content-Type': 'application/json'
        }
    };

    // セッショントークンをヘッダーに追加
    const token = getSessionToken();
    if (token) {
        opts.headers['X-Session-Token'] = token;
    }

    if (body) {
        opts.body = JSON.stringify(body);
    }

    try {
        const res = await fetch(API_BASE + url, opts);

        if (res.status === 401) {
            showDialog('セッションが切れました。再ログインしてください。', () => {
                clearSessionToken();
                clearUserInfo();
                location.href = '/Login';
            });
            return null;
        }

        const data = await res.json();

        if (!res.ok) {
            throw new Error(data.error || `APIエラー (${res.status})`);
        }

        // ネットワークエラーバナーを非表示
        hideNetError();

        return data;
    } catch (err) {
        if (err instanceof TypeError && err.message.includes('fetch')) {
            showNetError();
            throw new Error('ネットワーク接続を確認してください');
        }
        throw err;
    }
}

// --- Snackbar ---

let snackbarTimer = null;

/**
 * Snackbar表示
 * @param {string} message メッセージ
 * @param {string} type 'success' | 'error' | 'warning' | 'info'
 * @param {number} duration 表示時間(ms)
 */
function showSnackbar(message, type = 'info', duration = 3000) {
    let el = document.getElementById('snackbar');
    if (!el) {
        el = document.createElement('div');
        el.id = 'snackbar';
        el.className = 'snackbar';
        document.body.appendChild(el);
    }

    // 前回のタイマーをクリア
    if (snackbarTimer) clearTimeout(snackbarTimer);

    el.textContent = message;
    el.className = `snackbar ${type}`;

    // アニメーション再トリガー
    requestAnimationFrame(() => {
        el.classList.add('show');
    });

    snackbarTimer = setTimeout(() => {
        el.classList.remove('show');
    }, duration);
}

// --- ダイアログ ---

/**
 * ダイアログ表示
 * @param {string} message メッセージ
 * @param {Function|null} onOk OKボタン押下時のコールバック
 * @param {string} title タイトル
 */
function showDialog(message, onOk = null, title = '確認') {
    // 既存のダイアログを削除
    const existing = document.getElementById('app-dialog');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.id = 'app-dialog';
    overlay.className = 'dialog-overlay';
    overlay.innerHTML = `
        <div class="dialog">
            <div class="dialog-title">${escapeHtml(title)}</div>
            <div class="dialog-content">${escapeHtml(message)}</div>
            <div class="dialog-actions">
                <button class="btn btn-filled" id="dialog-ok-btn">OK</button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);

    requestAnimationFrame(() => overlay.classList.add('show'));

    document.getElementById('dialog-ok-btn').addEventListener('click', () => {
        overlay.classList.remove('show');
        setTimeout(() => overlay.remove(), 200);
        if (onOk) onOk();
    });
}

/**
 * 確認ダイアログ表示（OK/キャンセル）
 * @param {string} message メッセージ
 * @param {Function} onOk OKコールバック
 * @param {string} title タイトル
 */
function showConfirm(message, onOk, title = '確認') {
    const existing = document.getElementById('app-dialog');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.id = 'app-dialog';
    overlay.className = 'dialog-overlay';
    overlay.innerHTML = `
        <div class="dialog">
            <div class="dialog-title">${escapeHtml(title)}</div>
            <div class="dialog-content">${escapeHtml(message)}</div>
            <div class="dialog-actions">
                <button class="btn btn-text" id="dialog-cancel-btn">キャンセル</button>
                <button class="btn btn-filled" id="dialog-ok-btn">OK</button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);

    requestAnimationFrame(() => overlay.classList.add('show'));

    const close = () => {
        overlay.classList.remove('show');
        setTimeout(() => overlay.remove(), 200);
    };

    document.getElementById('dialog-ok-btn').addEventListener('click', () => {
        close();
        onOk();
    });
    document.getElementById('dialog-cancel-btn').addEventListener('click', close);
}

// --- ネットワークエラーバナー ---

function showNetError() {
    let banner = document.getElementById('net-error-banner');
    if (!banner) {
        banner = document.createElement('div');
        banner.id = 'net-error-banner';
        banner.className = 'net-error-banner';
        banner.innerHTML = '<span class="material-icons-round" style="vertical-align:middle;margin-right:8px;">wifi_off</span>ネットワーク接続を確認してください';
        document.body.prepend(banner);
    }
    banner.classList.add('show');
}

function hideNetError() {
    const banner = document.getElementById('net-error-banner');
    if (banner) banner.classList.remove('show');
}

// --- 音声フィードバック ---

let audioCtx = null;

/** AudioContextを取得（遅延初期化） */
function getAudioContext() {
    if (!audioCtx) {
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }
    return audioCtx;
}

/**
 * ビープ音再生
 * @param {number} freq 周波数(Hz)
 * @param {number} duration 長さ(ms)
 */
function playBeep(freq, duration) {
    try {
        const ctx = getAudioContext();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.frequency.value = freq;
        osc.type = 'sine';
        gain.gain.setValueAtTime(0.15, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration / 1000);
        osc.start();
        osc.stop(ctx.currentTime + duration / 1000);
    } catch (e) {
        console.warn('音声再生エラー:', e);
    }
}

/** スキャン成功音 (880Hz, 100ms) */
function playSuccessBeep() {
    playBeep(880, 100);
}

/** エラーブザー音 (220Hz, 300ms) */
function playErrorBuzz() {
    playBeep(220, 300);
}

/** 検品完了音 (ド・ミ・ソ) */
function playCompletionSound() {
    try {
        const ctx = getAudioContext();
        const notes = [523.25, 659.25, 783.99]; // ド・ミ・ソ
        notes.forEach((freq, i) => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.type = 'sine';
            osc.frequency.value = freq;
            gain.gain.setValueAtTime(0.15, ctx.currentTime + i * 0.15);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + i * 0.15 + 0.4);
            osc.start(ctx.currentTime + i * 0.15);
            osc.stop(ctx.currentTime + i * 0.15 + 0.4);
        });
    } catch (e) {
        console.warn('完了音再生エラー:', e);
    }
}

// --- ユーティリティ ---

/** HTMLエスケープ */
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

/** 日付フォーマット (YYYY-MM-DD) */
function formatDate(date) {
    if (!date) return '';
    const d = new Date(date);
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

/** 今日の日付文字列を取得 */
function getToday() {
    return formatDate(new Date());
}

/** N日後の日付文字列を取得 */
function getDateOffset(days) {
    const d = new Date();
    d.setDate(d.getDate() + days);
    return formatDate(d);
}

/** 日時フォーマット (YYYY-MM-DD HH:mm:ss) */
function formatDateTime(date) {
    if (!date) return '';
    const d = new Date(date);
    return `${formatDate(d)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`;
}

/** デバウンス */
function debounce(fn, delay) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => fn.apply(this, args), delay);
    };
}
