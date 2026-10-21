/* Source contract: assets.js defines window.PIXELLAB_CLIPS = { clips: [...] }.
   Optional phases: [{ name: '준비', startFrame: 1, endFrame: 4 }], 1-based inclusive.
   Frame cells and pivot use original sheet pixels; no per-frame normalization. */
(() => {
  'use strict';
  const ids = ['emptyState', 'reviewWorkspace', 'clipSelect', 'clipCount', 'actorAction', 'clipName', 'clipStatus', 'imageStatus', 'frameViewport', 'frameStack', 'frameCanvas', 'pixelGrid', 'pivotMarker', 'frameNumber', 'frameTime', 'phaseName', 'frameScrub', 'phaseMarks', 'firstFrame', 'previousFrame', 'playPause', 'nextFrame', 'loopToggle', 'speedSelect', 'scaleSelect', 'gridToggle', 'pivotToggle', 'frameSize', 'totalFrames', 'fpsValue', 'effectiveFps', 'pivotValue', 'sheetLayout', 'clipNote', 'sourceJob', 'sourcePath', 'comparisonSection', 'firstStack', 'firstCanvas', 'firstPivot', 'lastStack', 'lastCanvas', 'lastPivot', 'lastFrameLabel', 'sheetSection', 'downloadSheet', 'sheetCaption', 'contactSheet'];
  const ui = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
  const state = { clips: [], clip: null, image: null, frame: 0, playing: false, elapsed: 0, lastTime: null, raf: null, loadToken: 0, phases: [] };
  const controls = ['frameScrub', 'firstFrame', 'previousFrame', 'playPause', 'nextFrame', 'loopToggle', 'speedSelect', 'scaleSelect', 'gridToggle', 'pivotToggle'];

  function validateClip(clip) {
    if (!clip || !clip.id || typeof clip.sheet !== 'string' || !clip.sheet.trim()) return '클립 ID 또는 원본 시트 경로가 없습니다.';
    for (const key of ['frameWidth', 'frameHeight', 'columns', 'frameCount']) {
      if (!Number.isInteger(clip[key]) || clip[key] <= 0) return `${key} 값은 양의 정수여야 합니다.`;
    }
    if (!Number.isFinite(clip.fps) || clip.fps <= 0) return '재생 FPS 값이 올바르지 않습니다.';
    if (!Array.isArray(clip.pivot) || clip.pivot.length !== 2 || !clip.pivot.every(Number.isFinite)) return '공통 피벗 좌표가 없습니다.';
    if (clip.pivot[0] < 0 || clip.pivot[0] > clip.frameWidth || clip.pivot[1] < 0 || clip.pivot[1] > clip.frameHeight) return '피벗이 원본 프레임 범위를 벗어났습니다.';
    return '';
  }

  function frameRect(clip, frame) {
    return [(frame % clip.columns) * clip.frameWidth, Math.floor(frame / clip.columns) * clip.frameHeight, clip.frameWidth, clip.frameHeight];
  }

  function setControls(enabled) {
    for (const id of controls) ui[id].disabled = !enabled;
    if (enabled && state.clip.frameCount === 1) {
      for (const id of ['playPause', 'firstFrame', 'previousFrame', 'nextFrame', 'frameScrub']) ui[id].disabled = true;
    }
  }

  function pause() {
    state.playing = false;
    state.lastTime = null;
    state.elapsed = 0;
    if (state.raf !== null) cancelAnimationFrame(state.raf);
    state.raf = null;
    ui.playPause.textContent = '▶ 재생';
    ui.playPause.setAttribute('aria-pressed', 'false');
  }

  function drawFrame(canvas, frame) {
    const context = canvas.getContext('2d');
    context.clearRect(0, 0, canvas.width, canvas.height);
    context.imageSmoothingEnabled = false;
    context.drawImage(state.image, ...frameRect(state.clip, frame), 0, 0, state.clip.frameWidth, state.clip.frameHeight);
  }

  function placePivot(marker, scale) {
    marker.style.left = `${state.clip.pivot[0] * scale}px`;
    marker.style.top = `${state.clip.pivot[1] * scale}px`;
    marker.hidden = !ui.pivotToggle.checked;
  }

  function updateView() {
    if (!state.image) return;
    const scale = Number(ui.scaleSelect.value);
    ui.frameStack.style.width = `${state.clip.frameWidth * scale}px`;
    ui.frameStack.style.height = `${state.clip.frameHeight * scale}px`;
    ui.pixelGrid.style.setProperty('--pixel-size', `${scale}px`);
    ui.pixelGrid.hidden = !ui.gridToggle.checked;
    placePivot(ui.pivotMarker, scale);
    placePivot(ui.firstPivot, 2);
    placePivot(ui.lastPivot, 2);
  }

  function showFrame() {
    if (!state.image) return;
    const { clip, frame } = state;
    drawFrame(ui.frameCanvas, frame);
    ui.frameNumber.textContent = `${frame + 1} / ${clip.frameCount}`;
    ui.frameTime.textContent = `${(frame / clip.fps).toFixed(3)}s / ${(clip.frameCount / clip.fps).toFixed(3)}s`;
    ui.frameScrub.value = String(frame + 1);
    ui.frameScrub.setAttribute('aria-valuetext', `${clip.frameCount}개 중 ${frame + 1}번째 프레임`);
    ui.frameCanvas.setAttribute('aria-label', `${clip.name || clip.id}, 원본 프레임 ${frame + 1} / ${clip.frameCount}`);
    const phase = state.phases.find(item => frame + 1 >= item.startFrame && frame + 1 <= item.endFrame);
    ui.phaseName.hidden = !phase;
    ui.phaseName.textContent = phase ? phase.name : '';
    [...ui.phaseMarks.children].forEach((button, index) => button.setAttribute('aria-current', String(state.phases[index] === phase)));
  }

  function seek(frame) {
    if (!state.image) return;
    pause();
    state.frame = Math.max(0, Math.min(state.clip.frameCount - 1, frame));
    showFrame();
  }

  function tick(timestamp) {
    if (!state.playing) return;
    if (state.lastTime !== null) state.elapsed += timestamp - state.lastTime;
    state.lastTime = timestamp;
    const interval = 1000 / (state.clip.fps * Number(ui.speedSelect.value));
    const advance = Math.floor(state.elapsed / interval);
    if (advance > 0) {
      state.elapsed -= advance * interval;
      const next = state.frame + advance;
      if (ui.loopToggle.checked) state.frame = next % state.clip.frameCount;
      else if (next >= state.clip.frameCount) {
        state.frame = state.clip.frameCount - 1;
        pause();
      } else state.frame = next;
      showFrame();
    }
    if (state.playing) state.raf = requestAnimationFrame(tick);
  }

  function togglePlayback() {
    if (!state.image || state.clip.frameCount < 2) return;
    if (state.playing) return pause();
    if (!ui.loopToggle.checked && state.frame === state.clip.frameCount - 1) state.frame = 0;
    state.playing = true;
    state.lastTime = null;
    state.elapsed = 0;
    ui.playPause.textContent = 'Ⅱ 일시정지';
    ui.playPause.setAttribute('aria-pressed', 'true');
    showFrame();
    state.raf = requestAnimationFrame(tick);
  }

  function showError(message) {
    pause();
    state.image = null;
    setControls(false);
    ui.imageStatus.hidden = false;
    ui.imageStatus.classList.add('error');
    ui.imageStatus.textContent = message;
    ui.frameViewport.hidden = true;
    ui.comparisonSection.hidden = true;
    ui.sheetSection.hidden = true;
  }

  function updateSpeed() {
    if (!state.clip) return;
    const rate = state.clip.fps * Number(ui.speedSelect.value);
    ui.effectiveFps.textContent = `${Number(rate.toFixed(2))} FPS · ${ui.speedSelect.value}×`;
    state.lastTime = null;
    state.elapsed = 0;
  }

  function loadClip(index) {
    pause();
    const token = ++state.loadToken;
    const clip = state.clips[index];
    state.clip = clip;
    state.image = null;
    state.frame = 0;
    state.phases = [];
    setControls(false);
    ui.frameViewport.hidden = true;
    ui.comparisonSection.hidden = true;
    ui.sheetSection.hidden = true;
    ui.imageStatus.hidden = false;
    ui.imageStatus.classList.remove('error');
    ui.imageStatus.textContent = '원본 프레임 불러오는 중';
    ui.clipName.textContent = clip?.name || clip?.id || '클립 정보 오류';
    ui.actorAction.textContent = [clip?.actor, clip?.action].filter(Boolean).join(' / ');
    ui.clipStatus.textContent = ({ review: '검토 중', approved: '검토 완료', draft: '기획 후보', rejected: '반려 · 비교 보존' })[clip?.status] || clip?.status || '검토 중';
    ui.frameNumber.textContent = '—';
    ui.frameTime.textContent = '';
    ui.phaseName.hidden = true;
    ui.phaseMarks.hidden = true;
    ui.phaseMarks.replaceChildren();
    for (const id of ['frameSize', 'totalFrames', 'fpsValue', 'effectiveFps', 'pivotValue', 'sheetLayout']) ui[id].textContent = '—';
    ui.clipNote.textContent = clip?.note || '';
    ui.clipNote.hidden = !clip?.note;
    ui.sourceJob.textContent = clip?.sourceJob ? `생성 작업: ${clip.sourceJob}` : '생성 작업 ID 미기재';
    ui.sourcePath.textContent = clip?.sheet || '';
    const error = validateClip(clip);
    if (error) return showError(error);

    ui.loopToggle.checked = clip.loop === true;
    ui.frameScrub.max = String(clip.frameCount);
    ui.frameScrub.value = '1';
    ui.frameSize.textContent = `${clip.frameWidth} × ${clip.frameHeight}px`;
    ui.totalFrames.textContent = `${clip.frameCount}장`;
    ui.fpsValue.textContent = `${clip.fps} FPS · ${(clip.frameCount / clip.fps).toFixed(2)}초`;
    ui.pivotValue.textContent = `(${clip.pivot.join(', ')})px`;
    ui.sheetLayout.textContent = `${clip.columns}열 · ${Math.ceil(clip.frameCount / clip.columns)}행`;
    updateSpeed();
    state.phases = (Array.isArray(clip.phases) ? clip.phases : []).filter(phase => phase && typeof phase.name === 'string' && Number.isInteger(phase.startFrame) && Number.isInteger(phase.endFrame) && phase.startFrame >= 1 && phase.endFrame >= phase.startFrame && phase.endFrame <= clip.frameCount);

    const image = new Image();
    image.onload = () => {
      if (token !== state.loadToken) return;
      const width = clip.columns * clip.frameWidth;
      const height = Math.ceil(clip.frameCount / clip.columns) * clip.frameHeight;
      if (image.naturalWidth !== width || image.naturalHeight !== height) return showError(`시트 크기 불일치: 원본 ${image.naturalWidth}×${image.naturalHeight}px / 목록 기준 ${width}×${height}px. 셀 배치를 확인해 주세요.`);
      state.image = image;
      for (const canvas of [ui.frameCanvas, ui.firstCanvas, ui.lastCanvas]) {
        canvas.width = clip.frameWidth;
        canvas.height = clip.frameHeight;
      }
      for (const stack of [ui.firstStack, ui.lastStack]) {
        stack.style.width = `${clip.frameWidth * 2}px`;
        stack.style.height = `${clip.frameHeight * 2}px`;
      }
      ui.lastFrameLabel.textContent = String(clip.frameCount);
      drawFrame(ui.firstCanvas, 0);
      drawFrame(ui.lastCanvas, clip.frameCount - 1);
      ui.contactSheet.src = clip.sheet;
      ui.contactSheet.alt = `${clip.name || clip.id} 원본 전체 ${clip.frameCount}프레임 시트`;
      ui.contactSheet.width = image.naturalWidth;
      ui.contactSheet.height = image.naturalHeight;
      ui.downloadSheet.href = clip.sheet;
      ui.downloadSheet.download = `${clip.id}-sheet.png`;
      const blankCells = clip.columns * Math.ceil(clip.frameCount / clip.columns) - clip.frameCount;
      ui.sheetCaption.textContent = `왼쪽에서 오른쪽, 위에서 아래로 읽습니다. ${image.naturalWidth} × ${image.naturalHeight}px 원본.${blankCells ? ` 마지막 행의 빈 ${blankCells}칸은 재생하지 않습니다.` : ''}`;
      for (const phase of state.phases) {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = `${phase.name} ${phase.startFrame}–${phase.endFrame}`;
        button.addEventListener('click', () => seek(phase.startFrame - 1));
        ui.phaseMarks.append(button);
      }
      ui.phaseMarks.hidden = state.phases.length === 0;
      ui.imageStatus.hidden = true;
      ui.frameViewport.hidden = false;
      ui.comparisonSection.hidden = false;
      ui.sheetSection.hidden = false;
      setControls(true);
      updateView();
      showFrame();
    };
    image.onerror = () => { if (token === state.loadToken) showError('원본 시트를 불러오지 못했습니다. 파일 생성 상태와 경로를 확인한 뒤 페이지를 새로고침해 주세요.'); };
    image.src = clip.sheet;
  }

  ui.clipSelect.addEventListener('change', () => loadClip(Number(ui.clipSelect.value)));
  ui.playPause.addEventListener('click', togglePlayback);
  ui.firstFrame.addEventListener('click', () => seek(0));
  ui.previousFrame.addEventListener('click', () => seek(state.frame - 1));
  ui.nextFrame.addEventListener('click', () => seek(state.frame + 1));
  ui.frameScrub.addEventListener('input', () => seek(Number(ui.frameScrub.value) - 1));
  ui.speedSelect.addEventListener('change', updateSpeed);
  for (const id of ['scaleSelect', 'gridToggle', 'pivotToggle']) ui[id].addEventListener('change', updateView);
  ui.frameCanvas.addEventListener('keydown', event => {
    if (!state.image) return;
    const actions = { ArrowLeft: () => seek(state.frame - 1), ArrowRight: () => seek(state.frame + 1), Home: () => seek(0), End: () => seek(state.clip.frameCount - 1), ' ': togglePlayback };
    if (actions[event.key]) { event.preventDefault(); if (!event.repeat || event.key !== ' ') actions[event.key](); }
  });
  document.addEventListener('visibilitychange', () => { if (document.hidden) pause(); });
  window.addEventListener('pagehide', pause);

  const manifest = window.PIXELLAB_CLIPS;
  if (!manifest || !Array.isArray(manifest.clips) || manifest.clips.length === 0) return;
  state.clips = manifest.clips;
  ui.emptyState.hidden = true;
  ui.reviewWorkspace.hidden = false;
  ui.clipCount.textContent = `${state.clips.length}개 동작`;
  state.clips.forEach((clip, index) => {
    const option = document.createElement('option');
    option.value = String(index);
    option.textContent = `${clip?.actor === 'hero' && !clip.id.startsWith('hero-v9-') ? '[이전 후보] ' : ''}${clip?.status === 'rejected' ? '[반려 보존] ' : ''}${clip?.actor || '대상 미지정'} · ${clip?.name || clip?.id || '클립 정보 오류'}`;
    ui.clipSelect.append(option);
  });
  const initial = Math.max(0, state.clips.findIndex(clip => clip.id === 'hero-v9-sprint-authored'));
  ui.clipSelect.value = String(initial);
  loadClip(initial);
})();
