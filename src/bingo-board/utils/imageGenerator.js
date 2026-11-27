// Canvas drawing utilities for bingo board image generation
import aspireLogo from '../assets/aspire-logo-256.png';

export class BingoImageGenerator {
  constructor(currentBoard, bingoLines, isPartOfBingo) {
    this.currentBoard = currentBoard;
    this.bingoLines = bingoLines;
    this.isPartOfBingo = isPartOfBingo;
    this.scale = 2;
    this.canvasSize = 600;
    this.gridSize = 400;
    this.squareSize = (this.gridSize - 40) / 5;
    this.gap = 8;
  }

  async generateImage() {
    try {
      await document.fonts?.ready;
      const canvas = this.createCanvas();
      const ctx = canvas.getContext('2d');
      
      this.drawBackground(ctx);
      this.drawTitle(ctx);
      await this.drawGrid(ctx);
      this.drawFooter(ctx);
      
      return this.downloadCanvas(canvas);
    } catch (error) {
      console.error('Error generating image:', error);
      throw new Error(`Image generation failed: ${error.message}`);
    }
  }

  createCanvas() {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    
    canvas.width = this.canvasSize * this.scale;
    canvas.height = this.canvasSize * this.scale;
    ctx.scale(this.scale, this.scale);
    
    return canvas;
  }

  drawBackground(ctx) {
    ctx.fillStyle = '#0f0d1d';
    ctx.fillRect(0, 0, this.canvasSize, this.canvasSize);

    const glow = ctx.createRadialGradient(this.canvasSize / 2, -60, 0, this.canvasSize / 2, -60, 420);
    glow.addColorStop(0, 'rgba(116, 85, 221, 0.35)');
    glow.addColorStop(1, 'rgba(116, 85, 221, 0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, this.canvasSize, this.canvasSize);
  }

  drawTitle(ctx) {
    // Title
    ctx.fillStyle = '#ffffff';
    ctx.font = '700 34px Poppins, sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('AspiriFridays Bingo', this.canvasSize / 2, 50);
    
    // Bingo status
    if (this.bingoLines.length > 0) {
      ctx.fillStyle = '#b9aaee';
      ctx.font = '700 22px Poppins, sans-serif';
      ctx.fillText('Bingo!', this.canvasSize / 2, 85);
    }
  }

  async drawGrid(ctx) {
    const gridX = (this.canvasSize - this.gridSize) / 2;
    const gridY = 120;
    
    // Grid container background
    ctx.fillStyle = '#1a1830';
    this.roundRect(ctx, gridX - 10, gridY - 10, this.gridSize + 20, this.gridSize + 20, 15);
    ctx.fill();
    
    // Load Aspire logo
    const aspireImg = await this.loadAspireLogo();
    
    // Draw each square
    for (let i = 0; i < 25; i++) {
      await this.drawSquare(ctx, i, gridX, gridY, aspireImg);
    }
  }

  async loadAspireLogo() {
    try {
      const img = new Image();
      await new Promise((resolve, reject) => {
        const timeout = setTimeout(() => reject(new Error('Logo load timeout')), 3000);
        img.onload = () => {
          clearTimeout(timeout);
          resolve();
        };
        img.onerror = () => {
          clearTimeout(timeout);
          reject(new Error('Logo load failed'));
        };
        img.src = aspireLogo;
      });
      return img;
    } catch (error) {
      console.warn('Could not load Aspire logo, using fallback:', error);
      return null;
    }
  }

  async drawSquare(ctx, index, gridX, gridY, aspireImg) {
    const row = Math.floor(index / 5);
    const col = index % 5;
    const x = gridX + 10 + col * (this.squareSize + this.gap);
    const y = gridY + 10 + row * (this.squareSize + this.gap);
    const square = this.currentBoard[index];
    
    ctx.save();
    
    // Square background
    this.drawSquareBackground(ctx, x, y, square);
    
    // Square border and effects
    this.drawSquareBorder(ctx, x, y, index);
    
    // Square content
    this.drawSquareContent(ctx, x, y, square, aspireImg);
    
    ctx.restore();
  }

  drawSquareBackground(ctx, x, y, square) {
    if (square.marked) {
      ctx.fillStyle = square.type === 'free' ? '#dcd5f6' : '#7455dd';
    } else if (square.type === 'free') {
      ctx.fillStyle = '#dcd5f6';
    } else {
      ctx.fillStyle = '#221f3a';
    }
    
    this.roundRect(ctx, x, y, this.squareSize, this.squareSize, 10);
    ctx.fill();
  }

  drawSquareBorder(ctx, x, y, index) {
    const isBingoSquare = this.isPartOfBingo(index);
    
    if (isBingoSquare && this.currentBoard[index]?.type !== 'free') {
      ctx.fillStyle = this.createGradient(ctx, x, y, '#7455dd', '#b30f87');
      this.roundRect(ctx, x, y, this.squareSize, this.squareSize, 10);
      ctx.fill();
    }

    ctx.strokeStyle = isBingoSquare ? '#b30f87' : 'rgba(185, 170, 238, 0.14)';
    ctx.lineWidth = 1;
    this.roundRect(ctx, x, y, this.squareSize, this.squareSize, 10);
    ctx.stroke();
  }

  drawSquareContent(ctx, x, y, square, aspireImg) {
    if (square.type === 'free') {
      this.drawFreeSpaceContent(ctx, x, y, aspireImg);
    } else {
      this.drawTextContent(ctx, x, y, square);
    }
  }

  drawFreeSpaceContent(ctx, x, y, aspireImg) {
    if (aspireImg) {
      const logoSize = this.squareSize * 0.6;
      const logoX = x + (this.squareSize - logoSize) / 2;
      const logoY = y + (this.squareSize - logoSize) / 2;
      ctx.drawImage(aspireImg, logoX, logoY, logoSize, logoSize);
    } else {
      // Fallback text
      ctx.fillStyle = '#512bd4';
      ctx.font = '700 16px Poppins, sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText('FREE', x + this.squareSize/2, y + this.squareSize/2 - 4);
      ctx.font = '700 12px Poppins, sans-serif';
      ctx.fillText('SPACE', x + this.squareSize/2, y + this.squareSize/2 + 12);
    }
  }

  drawTextContent(ctx, x, y, square) {
    ctx.fillStyle = '#ffffff';
    ctx.font = square.marked ? '600 11px Poppins, sans-serif' : '500 11px Poppins, sans-serif';
    ctx.textAlign = 'center';
    
    const lines = this.wrapText(ctx, square.label, this.squareSize - 16);
    const lineHeight = 13;
    const startY = y + this.squareSize/2 - (lines.length - 1) * lineHeight/2 + 4;
    
    lines.forEach((line, index) => {
      ctx.fillText(line, x + this.squareSize/2, startY + index * lineHeight);
    });
  }

  wrapText(ctx, text, maxWidth) {
    const words = text.split(' ');
    const lines = [];
    let currentLine = words[0];
    
    for (let w = 1; w < words.length; w++) {
      const testLine = currentLine + ' ' + words[w];
      const metrics = ctx.measureText(testLine);
      if (metrics.width > maxWidth) {
        lines.push(currentLine);
        currentLine = words[w];
      } else {
        currentLine = testLine;
      }
    }
    lines.push(currentLine);
    return lines;
  }

  drawFooter(ctx) {
    ctx.fillStyle = '#a8a2c6';
    ctx.font = '12px Poppins, sans-serif';
    ctx.textAlign = 'center';
    
    const today = new Date().toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
    
    ctx.fillText(today, this.canvasSize / 2, 560);
    ctx.fillText('youtube.com/@aspiredotdev', this.canvasSize / 2, 580);
  }

  createGradient(ctx, x, y, color1, color2) {
    const gradient = ctx.createLinearGradient(x, y, x + this.squareSize, y + this.squareSize);
    gradient.addColorStop(0, color1);
    gradient.addColorStop(1, color2);
    return gradient;
  }

  roundRect(ctx, x, y, width, height, radius) {
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
  }

  downloadCanvas(canvas) {
    // Check if we're in a HybridWebView by looking for the HybridWebView object
    if (typeof window.HybridWebView !== 'undefined') {
      // In HybridWebView, ask the app to display the image
      return new Promise((resolve, reject) => {
        const data = canvas.toDataURL('image/png');
        window.HybridWebView.InvokeDotNet('DownloadBoard', data);
      });
    }
    
    return new Promise((resolve, reject) => {
      canvas.toBlob((blob) => {
        if (!blob) {
          reject(new Error('Failed to create blob from canvas'));
          return;
        }

        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `aspirifridays-bingo-${new Date().toISOString().split('T')[0]}.png`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        resolve();
      }, 'image/png');
    });
  }
}
