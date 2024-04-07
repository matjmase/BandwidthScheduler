export class TimeFrameModel {
  constructor(
    public StartTime: Date,
    public EndTime: Date,
    public Level: TriStateButton[]
  ) {}

  public SetLevelLeave() {
    for (let i = 0; i < this.Level.length; i++) {
      if (this.Level[i] === TriStateButton.Hovered) {
        this.Level[i] = TriStateButton.NotSelected;
      }
    }
  }

  public SetLevelHover(index: number) {
    for (let i = 0; i < this.Level.length; i++) {
      if (this.Level[i] !== TriStateButton.Selected) {
        this.Level[i] = TriStateButton.NotSelected;
      }
    }

    for (let i = 0; i <= index; i++) {
      if (this.Level[i] === TriStateButton.NotSelected) {
        this.Level[i] = TriStateButton.Hovered;
      }
    }
  }

  public SetLevelSelect(index: number) {
    const isActivating = this.Level[index] !== TriStateButton.Selected;

    for (let i = 0; i < this.Level.length; i++) {
      this.Level[i] = TriStateButton.NotSelected;
    }

    if (isActivating) {
      for (let i = 0; i <= index; i++) {
        this.Level[i] = TriStateButton.Selected;
      }
    }
  }
}

export enum TriStateButton {
  NotSelected = 0,
  Hovered = 1,
  Selected = 2,
}
