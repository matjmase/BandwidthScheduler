export class ColoredTimeFrameModel {
  constructor(
    public StartTime: Date,
    public EndTime: Date,
    public Color: IColorModel[]
  ) {}
}

export interface IColorModel {
  R: number;
  G: number;
  B: number;
}
