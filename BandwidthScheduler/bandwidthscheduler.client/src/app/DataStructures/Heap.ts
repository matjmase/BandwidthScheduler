export class Heap<T> {
  private readonly _elements: T[];
  private readonly _correctOrderCompare: (first: T, second: T) => boolean;

  constructor(compare: (first: T, second: T) => boolean) {
    this._elements = [];
    this._correctOrderCompare = compare;
  }

  private GetLeftChildIndex(elementIndex: number): number {
    return 2 * elementIndex + 1;
  }

  private GetRightChildIndex(elementIndex: number): number {
    return 2 * elementIndex + 2;
  }

  private GetParentIndex(elementIndex: number): number {
    return Math.floor((elementIndex - 1) / 2);
  }

  private HasLeftChild(elementIndex: number): boolean {
    return this.GetLeftChildIndex(elementIndex) < this._elements.length;
  }

  private HasRightChild(elementIndex: number): boolean {
    return this.GetRightChildIndex(elementIndex) < this._elements.length;
  }

  private IsRoot(elementIndex: number) {
    return elementIndex === 0;
  }

  private GetLeftChild(elementIndex: number): T {
    return this._elements[this.GetLeftChildIndex(elementIndex)];
  }

  private GetRightChild(elementIndex: number): T {
    return this._elements[this.GetRightChildIndex(elementIndex)];
  }

  private GetParent(elementIndex: number): T {
    return this._elements[this.GetParentIndex(elementIndex)];
  }

  private Swap(firstIndex: number, secondIndex: number): void {
    var temp = this._elements[firstIndex];
    this._elements[firstIndex] = this._elements[secondIndex];
    this._elements[secondIndex] = temp;
  }

  public Length(): number {
    return this._elements.length;
  }

  public Peek(): T {
    if (this._elements.length == 0) throw new RangeError();

    return this._elements[0];
  }

  public Pop(): T {
    if (this._elements.length == 0) throw new RangeError();

    var result = this._elements[0];
    this._elements[0] = this._elements[this._elements.length - 1];
    this._elements.splice(this._elements.length - 1, 1);

    this.ReCalculateDown(0);
    return result;
  }

  public Add(element: T): void {
    this._elements.push(element);

    this.ReCalculateUp(this._elements.length - 1);
  }

  private ReCalculateDown(index: number): number {
    while (this.HasLeftChild(index)) {
      let smallerIndex = this.GetLeftChildIndex(index);
      if (
        this.HasRightChild(index) &&
        this._correctOrderCompare(
          this.GetRightChild(index),
          this.GetLeftChild(index)
        )
      ) {
        smallerIndex = this.GetRightChildIndex(index);
      }

      if (
        !this._correctOrderCompare(
          this._elements[smallerIndex],
          this._elements[index]
        )
      ) {
        break;
      }

      this.Swap(smallerIndex, index);
      index = smallerIndex;
    }

    return index;
  }

  private ReCalculateUp(index: number): number {
    while (
      !this.IsRoot(index) &&
      this._correctOrderCompare(this._elements[index], this.GetParent(index))
    ) {
      const parentIndex = this.GetParentIndex(index);
      this.Swap(parentIndex, index);
      index = parentIndex;
    }

    return index;
  }
}
