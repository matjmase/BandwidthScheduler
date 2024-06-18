import {
  Component,
  EventEmitter,
  OnChanges,
  Output,
  SimpleChanges,
  forwardRef,
} from '@angular/core';
import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  NgForm,
} from '@angular/forms';
import { IDateRangeSelectorModel } from './IDateRangeSelectorModel';
import { TimePickerModel } from '../../models/TimePickerModel';

@Component({
  selector: 'app-date-range-selector',
  templateUrl: './date-range-selector.component.html',
  styleUrl: './date-range-selector.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateRangeSelectorComponent),
      multi: true,
    },
  ],
})
export class DateRangeSelectorComponent implements ControlValueAccessor {
  public date1: Date | undefined;
  public time1: TimePickerModel = new TimePickerModel();
  public date2: Date | undefined;
  public time2: TimePickerModel = new TimePickerModel();

  private ApplyModel(model: IDateRangeSelectorModel): void {
    this.date1 = model.start;
    this.time1 = TimePickerModel.FromTime(
      model.start.getHours(),
      model.start.getMinutes(),
      0
    );

    this.date2 = model.end;
    this.time2 = TimePickerModel.FromTime(
      model.end.getHours(),
      model.end.getMinutes(),
      0
    );
  }

  public IsDisabled: boolean = false;

  private onChange = (model: IDateRangeSelectorModel | undefined) => {};

  onTouched = () => {};

  writeValue(obj: IDateRangeSelectorModel | undefined): void {
    if (obj) {
      this.ApplyModel(obj);
    }
  }
  registerOnChange(
    fn: (model: IDateRangeSelectorModel | undefined) => {}
  ): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
  setDisabledState?(isDisabled: boolean): void {
    this.IsDisabled = isDisabled;
  }

  public ModelChanged(): void {
    this.onTouched();

    if (this.date1 && this.date2) {
      const start = this.time1.TransformDate(this.date1);
      const end = this.time2.TransformDate(this.date2);

      const output: IDateRangeSelectorModel = {
        start: start,
        end: end,
      };

      this.onChange(output);
    }
  }
}
