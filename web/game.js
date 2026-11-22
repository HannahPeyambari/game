(() => {
  const canvas = document.getElementById('game');
  const ctx = canvas.getContext('2d');
  const overlay = document.getElementById('overlay');
  const lanes = [
    { name: 'SKY RUNNER', color: '#44d4f2', key: 'upper', score: 0, alive: true, obstacles: [], runnerY: 0, vy: 0, jumping: false, spawn: 0, speed: 275, passed: 0 },
    { name: 'SUNSET RUNNER', color: '#ff895d', key: 'lower', score: 0, alive: true, obstacles: [], runnerY: 0, vy: 0, jumping: false, spawn: 0.4, speed: 300, passed: 0 }
  ];
  const bestEl = document.getElementById('bestScore');
  let last = 0, time = 0, gameOverShown = false, best = Number(localStorage.getItem('twindash-best') || 0);
  bestEl.textContent = String(best).padStart(3, '0');

  function resize() {
    const box = canvas.getBoundingClientRect();
    const dpr = Math.min(devicePixelRatio || 1, 2);
    canvas.width = Math.round(box.width * dpr);
    canvas.height = Math.round(box.height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }
  new ResizeObserver(resize).observe(canvas);
  resize();
  const W = () => canvas.clientWidth, H = () => canvas.clientHeight;
  function ground(lane) { return H() * (lane === 0 ? .48 : .91); }
  function runnerX() { return Math.max(65, W() * .13); }

  function jump(index) {
    const l = lanes[index];
    if (!l.alive) return;
    const floor = ground(index);
    if (l.runnerY >= floor - 2) {
      l.vy = -575;
      l.jumping = true;
    }
  }
  function restart() {
    lanes.forEach((l, i) => Object.assign(l, { score: 0, alive: true, obstacles: [], runnerY: ground(i), vy: 0, jumping: false, spawn: .8 + i * .25, speed: 275 + i * 25, passed: 0 }));
    overlay.classList.add('hidden');
    gameOverShown = false;
    last = performance.now();
  }

  window.addEventListener('keydown', e => {
    if (['Space', 'ArrowUp', 'KeyW', 'KeyR'].includes(e.code)) e.preventDefault();
    if (e.code === 'Space' || e.code === 'KeyW') jump(0);
    if (e.code === 'ArrowUp') jump(1);
    if (e.code === 'KeyR') restart();
  });
  canvas.addEventListener('pointerdown', e => {
    const r = canvas.getBoundingClientRect();
    jump(e.clientY - r.top < r.height / 2 ? 0 : 1);
    canvas.focus();
  });
  document.getElementById('restartTop').addEventListener('click', restart);
  document.getElementById('restartOverlay').addEventListener('click', restart);

  function roundRect(x, y, w, h, r, fill) {
    ctx.beginPath(); ctx.roundRect(x, y, w, h, r); ctx.fillStyle = fill; ctx.fill();
  }
  function drawLane(i, dt) {
    const l = lanes[i], h = H(), w = W(), floor = ground(i), top = i === 0 ? 0 : h * .5;
    const laneHeight = h * .5;
    const bg = ctx.createLinearGradient(0, top, w, top + laneHeight);
    bg.addColorStop(0, i === 0 ? '#172443' : '#222039');
    bg.addColorStop(1, i === 0 ? '#121a31' : '#181a31');
    ctx.fillStyle = bg; ctx.fillRect(0, top, w, laneHeight);
    // Soft ambient color pools
    ctx.fillStyle = i === 0 ? '#44d4f208' : '#ff895d08';
    ctx.beginPath(); ctx.ellipse(w * .82, top + laneHeight * .28, w * .28, laneHeight * .52, 0, 0, Math.PI * 2); ctx.fill();
    // Horizon and runway
    ctx.fillStyle = '#ffffff0a'; ctx.fillRect(0, floor + 1, w, h - floor);
    ctx.fillStyle = l.color; ctx.globalAlpha = .82; ctx.fillRect(0, floor, w, 2); ctx.globalAlpha = 1;
    ctx.fillStyle = '#ffffff16';
    for (let x = -40 + ((time * l.speed * .45) % 100); x < w; x += 100) ctx.fillRect(x, floor + 13, 45, 2);
    // Glowing marker / small decorations
    for (let j = 0; j < 5; j++) {
      const x = (j * 177 + (w - time * 18) % (w + 177)) % w;
      ctx.fillStyle = '#ffffff0b'; ctx.fillRect(x, top + 27 + j % 2 * 16, 2, 2);
    }

    if (l.alive) {
      l.spawn -= dt;
      if (l.spawn <= 0) {
        const oh = 31 + Math.random() * 16;
        l.obstacles.push({ x: w + 20, width: 23, height: oh, scored: false });
        l.spawn = Math.max(.9, 1.55 + Math.random() * .72 - l.passed * .025);
      }
      l.obstacles.forEach(o => { o.x -= l.speed * dt; });
      l.obstacles = l.obstacles.filter(o => o.x > -40);
      l.obstacles.forEach(o => {
        // polished little block obstacle
        ctx.shadowColor = '#ff6178'; ctx.shadowBlur = 14;
        roundRect(o.x, floor - o.height, o.width, o.height, 5, '#ff6178');
        ctx.shadowBlur = 0;
        roundRect(o.x + 4, floor - o.height + 5, 4, Math.max(8, o.height - 10), 2, '#ffbbc3');
        if (!o.scored && o.x + o.width < runnerX()) { o.scored = true; l.score += 1; l.passed += 1; l.speed = Math.min(425, l.speed + 3); }
      });
      // Collision test: intentionally forgiving at the edges
      const px = runnerX(), py = l.runnerY;
      for (const o of l.obstacles) {
        if (px + 25 > o.x + 4 && px + 10 < o.x + o.width - 2 && py > floor - o.height - 27 && py < floor - 1) {
          l.alive = false;
        }
      }
      l.score += dt * 2.25;
    }

    // Runner shadow and bouncy little character
    const px = runnerX(), py = l.runnerY;
    ctx.globalAlpha = Math.max(.16, 1 - (floor - py) / 180);
    ctx.fillStyle = '#080d1a'; ctx.beginPath(); ctx.ellipse(px + 18, floor + 2, 20, 4, 0, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1;
    ctx.save(); ctx.translate(px, py - 28); ctx.rotate(l.jumping ? -.12 : Math.sin(time * 9 + i) * .025);
    ctx.shadowColor = l.color; ctx.shadowBlur = 18;
    roundRect(0, 0, 35, 34, 9, l.color); ctx.shadowBlur = 0;
    roundRect(7, 8, 5, 6, 2, '#11213b'); roundRect(20, 8, 5, 6, 2, '#11213b');
    roundRect(10, 23, 15, 3, 2, '#ffffffaa');
    ctx.restore();

    // Lane label
    ctx.fillStyle = '#ffffff'; ctx.globalAlpha = .42; ctx.font = '600 9px "DM Mono", monospace'; ctx.letterSpacing = '1.3px';
    ctx.fillText(i === 0 ? '01  /  SKY RUNNER' : '02  /  SUNSET RUNNER', 20, top + 22); ctx.globalAlpha = 1;
    if (!l.alive) {
      ctx.fillStyle = '#ff8b9b'; ctx.font = '700 9px "DM Mono", monospace'; ctx.fillText('CRASHED  ·  PRESS R TO RETRY', Math.max(20, w - 235), top + 22);
    }
  }

  function frame(now) {
    const dt = Math.min(.035, (now - last) / 1000 || 0); last = now; time += dt;
    const w = W(), h = H();
    ctx.clearRect(0, 0, w, h);
    lanes.forEach((l, i) => {
      const floor = ground(i);
      if (l.runnerY === 0) l.runnerY = floor;
      if (l.alive) {
        l.vy += 1500 * dt;
        l.runnerY = Math.min(floor, l.runnerY + l.vy * dt);
        if (l.runnerY >= floor) { l.vy = 0; l.jumping = false; }
      }
    });
    drawLane(0, dt);
    ctx.fillStyle = '#53607d'; ctx.fillRect(0, h * .5 - 1, w, 2);
    drawLane(1, dt);
    updateUI();
    if (!lanes[0].alive && !lanes[1].alive && !gameOverShown) {
      gameOverShown = true; overlay.classList.remove('hidden');
    }
    requestAnimationFrame(frame);
  }
  function updateUI() {
    const sky = Math.floor(lanes[0].score), sun = Math.floor(lanes[1].score);
    document.getElementById('skyScore').textContent = String(sky).padStart(3, '0');
    document.getElementById('sunScore').textContent = String(sun).padStart(3, '0');
    document.getElementById('skyStatus').textContent = lanes[0].alive ? 'RUNNING' : 'CRASHED';
    document.getElementById('sunStatus').textContent = lanes[1].alive ? 'RUNNING' : 'CRASHED';
    document.getElementById('skyStatus').style.color = lanes[0].alive ? '#73dfb5' : '#ff7183';
    document.getElementById('sunStatus').style.color = lanes[1].alive ? '#73dfb5' : '#ff7183';
    const current = Math.max(sky, sun);
    if (current > best) { best = current; localStorage.setItem('twindash-best', best); bestEl.textContent = String(best).padStart(3, '0'); }
  }
  restart();
  requestAnimationFrame(frame);
})();
