import {
  AfterContentChecked,
  AfterViewChecked,
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  OnInit,
  SimpleChanges,
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
export class TimePickerSnapComponent
  implements ControlValueAccessor, OnChanges
{
  @Input() public Step: number = 30;

  private _timeModel: TimePickerModel = new TimePickerModel();

  public set TimeModel(model: TimePickerModel) {
    const newModel = new TimePickerModel();

    const minMod = model.minute % this.Step;
    const proportion = minMod / this.Step;
    const round = Math.round(proportion);

    const minuteRounded = model.minute - minMod + round * this.Step;

    newModel.hour = model.hour;
    newModel.minute = minuteRounded;
    newModel.second = model.second;
    this._timeModel = newModel;
  }

  public get TimeModel(): TimePickerModel {
    return this._timeModel;
  }

  public IsDisabled: boolean = false;

  private onChange = (model: TimePickerModel | undefined) => {};

  private onTouched = () => {};

  writeValue(model: TimePickerModel | undefined): void {
    if (model) {
      this.TimeModel = model;
    } else {
      this.TimeModel = new TimePickerModel();
      this.onChange(this.TimeModel);
    }
  }
  registerOnChange(fn: (model: TimePickerModel | undefined) => {}): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
  setDisabledState?(isDisabled: boolean): void {
    this.IsDisabled = isDisabled;
  }

  pickerChanges(newModel: TimePickerModel): void {
    this.TimeModel = newModel;
    this.onChange(this.TimeModel);
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log(this.TimeModel);
  }
}
