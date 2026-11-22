/* Progressive enhancements: the page and video link work without JavaScript. */
(() => {
  const scenes = {
    classic: {
      title: 'START WITH A FAMILIAR DECK',
      copy: 'Shuffle, deal, and move cards around the table.',
      cards: [['J', '♣', 'J', 'Jack of clubs'], ['A', '♥', 'A', 'Ace of hearts'], ['K', '♠', 'K', 'King of spades'], ['Q', '♦', 'Q', 'Queen of diamonds']]
    },
    custom: {
      title: 'MAKE THE TABLE YOUR OWN',
      copy: 'Bring custom cards and play the game you imagine.',
      cards: [['Moon', '☾', 'Concept'], ['Forest', '✦', 'Concept'], ['Storm', '≈', 'Concept'], ['Ember', '✧', 'Concept']]
    },
    prototype: {
      title: 'TRY YOUR NEXT IDEA',
      copy: 'Test a deck with friends. Adjust it. Play another round.',
      cards: [['Idea 4', '☆', 'Draft'], ['Idea 1', '?', 'Draft'], ['Idea 2', '+', 'Draft'], ['Idea 3', '!', 'Draft']]
    }
  };

  const options = document.querySelector('.table-options');
  const table = document.querySelector('.table-preview');
  if (options && table) {
    const spread = table.querySelector('.card-spread');
    const states = new Map();
    let kind = 'classic';
    let selected = 1;
    let drag = null;
    let lastTap = null;
    let pointerType = 'mouse';
    let suppressDoubleClick = false;
    const cards = Array.from(spread.children, () => {
      const card = document.createElement('button');
      card.type = 'button';
      card.setAttribute('aria-describedby', 'table-instructions table-keyboard-help');
      return card;
    });

    function getState() {
      if (!states.has(kind)) {
        states.set(kind, cards.map((_, index) => ({
          x: 0.08 + index * 0.28,
          y: [0.5, 0.44, 0.5, 0.55][index],
          facedown: index === 0,
          layer: index
        })));
      }
      return states.get(kind);
    }

    // Store positions as fractions of the available space so resizing keeps cards on the table.
    function bounds(card) {
      return {
        x: Math.max(0, spread.clientWidth - card.offsetWidth - 12),
        y: Math.max(0, spread.clientHeight - card.offsetHeight - 12)
      };
    }

    function placeCards() {
      cards.forEach((card, index) => {
        const state = getState()[index];
        const limit = bounds(card);
        card.style.left = `${6 + state.x * limit.x}px`;
        card.style.top = `${6 + state.y * limit.y}px`;
        card.style.zIndex = state.layer;
        card.classList.toggle('is-selected', index === selected);
      });
    }

    function selectCard(index) {
      selected = index;
      // Keep a bounded stacking order without changing keyboard focus order.
      const state = getState();
      const previousLayer = state[index].layer;
      state.forEach(item => { if (item.layer > previousLayer) item.layer--; });
      state[index].layer = cards.length - 1;
      placeCards();
    }

    function renderCard(index) {
      const card = cards[index];
      const data = scenes[kind].cards[index];
      const facedown = getState()[index].facedown;
      card.className = 'playing-card choice-card' +
        (kind === 'classic' ? (index % 2 ? ' red' : '') : ' ' + kind) +
        (facedown ? ' is-facedown' : '');
      const visibleLabel = facedown ? 'CGS' : data.slice(0, 3).join(' ');
      card.setAttribute('aria-label', `${visibleLabel}, ${data[3] || data[0]}, face ${facedown ? 'down' : 'up'}`);
      if (facedown) {
        card.textContent = 'CGS';
      } else {
        const symbol = document.createElement('span');
        symbol.textContent = data[1];
        const label = document.createElement('small');
        label.textContent = data[2];
        card.replaceChildren(document.createTextNode(data[0]), symbol, label);
      }
    }

    function flipCard(index) {
      getState()[index].facedown = !getState()[index].facedown;
      renderCard(index);
      selectCard(index);
    }

    function moveCard(index, x, y) {
      const limit = bounds(cards[index]);
      getState()[index].x = limit.x ? Math.max(0, Math.min(1, (x - 6) / limit.x)) : 0;
      getState()[index].y = limit.y ? Math.max(0, Math.min(1, (y - 6) / limit.y)) : 0;
      placeCards();
    }

    function endDrag() {
      if (!drag) return;
      const { index, pointerId } = drag;
      drag = null;
      cards[index].classList.remove('is-dragging');
      if (cards[index].hasPointerCapture(pointerId)) cards[index].releasePointerCapture(pointerId);
    }

    cards.forEach((card, index) => {
      card.addEventListener('pointerdown', event => {
        if (event.button !== 0 || !event.isPrimary || drag) return;
        pointerType = event.pointerType;
        suppressDoubleClick = false;
        selectCard(index);
        card.focus({ preventScroll: true });
        drag = {
          index, pointerId: event.pointerId, x: event.clientX, y: event.clientY,
          left: card.offsetLeft, top: card.offsetTop, moved: false
        };
        card.setPointerCapture(event.pointerId);
      });
      card.addEventListener('pointermove', event => {
        if (!drag || drag.pointerId !== event.pointerId) return;
        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (!drag.moved && Math.hypot(dx, dy) < 5) return;
        drag.moved = true;
        suppressDoubleClick = true;
        lastTap = null;
        card.classList.add('is-dragging');
        moveCard(index, drag.left + dx, drag.top + dy);
      });
      card.addEventListener('pointerup', event => {
        if (!drag || drag.pointerId !== event.pointerId) return;
        if (!drag.moved && event.pointerType !== 'mouse') {
          const now = performance.now();
          if (lastTap && lastTap.index === index && now - lastTap.time < 400 &&
              Math.hypot(event.clientX - lastTap.x, event.clientY - lastTap.y) < 24) {
            flipCard(index);
            lastTap = null;
          } else {
            lastTap = { index, time: now, x: event.clientX, y: event.clientY };
          }
        }
        endDrag();
      });
      const cancelDrag = event => {
        if (drag && drag.pointerId === event.pointerId) {
          lastTap = null;
          endDrag();
        }
      };
      card.addEventListener('lostpointercapture', cancelDrag);
      card.addEventListener('pointercancel', cancelDrag);
      card.addEventListener('dblclick', event => {
        event.preventDefault();
        // Use the system's double-click timing for a mouse; touch/pen taps are handled above.
        if (pointerType === 'mouse' && !suppressDoubleClick) flipCard(index);
      });
      card.addEventListener('dragstart', event => event.preventDefault());
      card.addEventListener('focus', () => selectCard(index));
      // Native keyboard and assistive-technology activation produces a click with detail 0.
      card.addEventListener('click', event => {
        if (event.detail === 0) flipCard(index);
      });
      card.addEventListener('keydown', event => {
        const directions = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
        const direction = directions[event.key];
        if (!direction) return;
        event.preventDefault();
        lastTap = null;
        const step = event.shiftKey ? 30 : 10;
        moveCard(index, card.offsetLeft + direction[0] * step, card.offsetTop + direction[1] * step);
      });
    });

    spread.replaceChildren(...cards);
    spread.classList.add('interactive');
    spread.removeAttribute('aria-hidden');
    spread.setAttribute('role', 'group');
    spread.setAttribute('aria-label', 'Interactive tabletop');
    spread.addEventListener('click', event => {
      if (event.target !== spread) return;
      const rect = spread.getBoundingClientRect();
      const card = cards[selected];
      lastTap = null;
      moveCard(selected, event.clientX - rect.left - spread.clientLeft - card.offsetWidth / 2,
        event.clientY - rect.top - spread.clientTop - card.offsetHeight / 2);
    });
    table.querySelector('[data-flip-card]').addEventListener('click', () => {
      lastTap = null;
      flipCard(selected);
    });
    table.querySelector('[data-reset-table]').addEventListener('click', () => {
      endDrag();
      lastTap = null;
      states.delete(kind);
      cards.forEach((_, index) => renderCard(index));
      selectCard(1);
    });
    table.querySelector('.table-copy > small').hidden = true;
    table.querySelector('.table-instructions').hidden = false;
    cards.forEach((_, index) => renderCard(index));
    selectCard(selected);
    new ResizeObserver(() => { endDrag(); placeCards(); }).observe(spread);

    options.querySelectorAll('[data-deck]').forEach(button => {
      button.addEventListener('click', () => {
        const scene = scenes[button.dataset.deck];
        if (!scene) return;
        endDrag();
        lastTap = null;
        kind = button.dataset.deck;
        table.querySelector('[data-table-title]').textContent = scene.title;
        table.querySelector('[data-table-copy]').textContent = scene.copy;
        cards.forEach((_, index) => renderCard(index));
        placeCards();
        options.querySelectorAll('[data-deck]').forEach(option => {
          option.setAttribute('aria-pressed', String(option === button));
        });
      });
    });
    options.hidden = false;
  }

  const videoPlayer = document.querySelector('[data-video-player]');
  const videoLink = document.querySelector('[data-video-trigger]');
  if (videoPlayer && videoLink) {
    // A native button supports both Enter and Space. The original link is the no-JS fallback.
    const button = document.createElement('button');
    button.type = 'button';
    button.className = videoLink.className;
    button.append(...videoLink.childNodes);
    videoLink.replaceWith(button);

    button.addEventListener('click', () => {
      const iframe = document.createElement('iframe');
      iframe.src = 'https://www.youtube-nocookie.com/embed/PriDuaM6MEk';
      iframe.title = 'Card Game Simulator demo video';
      iframe.allow = 'encrypted-media; picture-in-picture; fullscreen';
      iframe.allowFullscreen = true;
      videoPlayer.textContent = '';
      videoPlayer.appendChild(iframe);
      iframe.focus();
    }, { once: true });
  }
})();
