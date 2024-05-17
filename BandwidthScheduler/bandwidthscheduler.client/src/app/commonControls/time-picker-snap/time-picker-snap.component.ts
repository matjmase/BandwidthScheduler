import {
  AfterContentChecked,
  AfterViewChecked,
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  Input,
  OnInit,
  forwardRef,
} from '@angular/core';
import { TimePickerModel } from '../../models/TimePickerModel';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-time-picker-snap',
  templateUrl: './time-picker-snap.component.html',
  styleUrl: './time-picker-snap.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TimePickerSnapComponent),
      multi: true,
    },
  ],
})
export class TimePickerSnapComponent {
  @Input() public Step: number = 30;

  @Input() public set Hours(val: number) {
    const clone = this.TimeModel.Clone();
    clone.hour = val;
    this.TimeModel = clone;
  }
  @Input() public set Minutes(val: number) {
    const clone = this.TimeModel.Clone();
    clone.minute = val;
    this.TimeModel = clone;
  }
  @Input() public set Seconds(val: number) {
    const clone = this.TimeModel.Clone();
    clone.second = val;
    this.TimeModel = clone;
  }

  private _timeModel: TimePickerModel = new TimePickerModel();

  public set TimeModel(model: TimePickerModel) {
    if (!model) return;

    const newModel = new TimePickerModel();

    const minMod = model.minute % this.Step;
    const proportion = minMod / this.Step;
    const round = Math.round(proportion);

    const minuteRounded = model.minute - minMod + round * this.Step;

    newModel.hour = model.hour;
    newModel.minute = minuteRounded;
    newModel.second = model.second;
    this._timeModel = newModel;
    this.onChange(this.TimeModel);
  }

  public get TimeModel() {
    return this._timeModel;
  }

  public IsDisabled: boolean = false;

  // Function to call when the rating changes.
  private onChange = (model: TimePickerModel) => {};

  // Function to call when the input is touched (when a star is clicked).
  private onTouched = () => {};

  writeValue(model: TimePickerModel | undefined): void {
    if (!model) {
      this.TimeModel = new TimePickerModel();
    } else {
      this.TimeModel = model;
    }
  }
  registerOnChange(fn: any): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
  setDisabledState?(isDisabled: boolean): void {
    this.IsDisabled = isDisabled;
  }
}
