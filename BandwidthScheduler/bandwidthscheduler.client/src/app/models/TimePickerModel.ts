export class TimePickerModel {
  public hour: number;
  public minute: number;
  public second: number;

  constructor() {
    this.hour = 0;
    this.minute = 0;
    this.second = 0;
  }

  public static FromTime(hour: number, minute: number, second: number) {
    const output = new TimePickerModel();

    output.hour = hour;
    output.minute = minute;
    output.second = second;

    return output;
  }

  public TransformDate(day: Date): Date {
    const output = new Date(day);

    output.setHours(this.hour, this.minute, this.second);

    return output;
  }

  public Clone(): TimePickerModel {
    const clone = new TimePickerModel();

    clone.hour = this.hour;
    clone.minute = this.minute;
    clone.second = this.second;

    return clone;
  }
}
