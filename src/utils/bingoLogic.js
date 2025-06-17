// Bingo game logic utilities
export class BingoGameLogic {
  static BINGO_LINES = [
    // Rows
    [0, 1, 2, 3, 4],
    [5, 6, 7, 8, 9],
    [10, 11, 12, 13, 14],
    [15, 16, 17, 18, 19],
    [20, 21, 22, 23, 24],
    // Columns
    [0, 5, 10, 15, 20],
    [1, 6, 11, 16, 21],
    [2, 7, 12, 17, 22],
    [3, 8, 13, 18, 23],
    [4, 9, 14, 19, 24],
    // Diagonals
    [0, 6, 12, 18, 24],
    [4, 8, 12, 16, 20]
  ];

  static FREE_SPACE_INDEX = 12;
  static BOARD_SIZE = 25;
  static SQUARES_PER_ROW = 5;

  static generateBoard(allSquares) {
    const freeSpace = allSquares.find(s => s.type === 'free');
    const otherSquares = allSquares.filter(s => s.type !== 'free');
    
    const shuffled = [...otherSquares].sort(() => Math.random() - 0.5);
    const selected = shuffled.slice(0, 24);
    
    const board = [];
    for (let i = 0; i < this.BOARD_SIZE; i++) {
      if (i === this.FREE_SPACE_INDEX) {
        board.push({ ...freeSpace, marked: true });
      } else {
        const squareIndex = i < this.FREE_SPACE_INDEX ? i : i - 1;
        board.push({ ...selected[squareIndex], marked: false });
      }
    }
    
    return board;
  }

  static checkForBingo(board) {
    return this.BINGO_LINES.filter(line => 
      line.every(index => board[index].marked)
    );
  }

  static isPartOfBingo(index, bingoLines) {
    return bingoLines.some(line => line.includes(index));
  }

  static getSquareAriaLabel(square, index) {
    const row = Math.floor(index / this.SQUARES_PER_ROW) + 1;
    const col = (index % this.SQUARES_PER_ROW) + 1;
    const position = `Row ${row}, Column ${col}`;
    
    if (square.type === 'free') {
      return `${position}, Free Space with .NET Aspire logo`;
    }
    
    const status = square.marked ? 'selected' : 'not selected';
    return `${position}, ${square.label}, ${status}`;
  }
}

// Keyboard navigation utilities
export class KeyboardNavigation {
  static GRID_SIZE = 5;
  static TOTAL_SQUARES = 25;

  static handleArrowKey(currentIndex, direction) {
    switch (direction) {
      case 'ArrowRight':
        return (currentIndex % this.GRID_SIZE === 4) 
          ? currentIndex - 4 
          : currentIndex + 1;
      case 'ArrowLeft':
        return (currentIndex % this.GRID_SIZE === 0) 
          ? currentIndex + 4 
          : currentIndex - 1;
      case 'ArrowDown':
        return (currentIndex + this.GRID_SIZE) % this.TOTAL_SQUARES;
      case 'ArrowUp':
        return (currentIndex - this.GRID_SIZE + this.TOTAL_SQUARES) % this.TOTAL_SQUARES;
      case 'Home':
        return 0;
      case 'End':
        return this.TOTAL_SQUARES - 1;
      default:
        return currentIndex;
    }
  }
}
