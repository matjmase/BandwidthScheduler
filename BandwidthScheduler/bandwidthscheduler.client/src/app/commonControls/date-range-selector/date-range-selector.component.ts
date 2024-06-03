import { Component, EventEmitter, Output } from '@angular/core';
import { ControlValueAccessor, NgForm } from '@angular/forms';
import { IDateRangeSelectorModel } from './IDateRangeSelectorModel';
import { TimePickerModel } from '../../models/TimePickerModel';

@Component({
  selector: 'app-date-range-selector',
  templateUrl: './date-range-selector.component.html',
  styleUrl: './date-range-selector.component.scss',
})
export class DateRangeSelectorComponent implements ControlValueAccessor {
  @Output()
  public DateRangeSelected: EventEmitter<IDateRangeSelectorModel> =
    new EventEmitter<IDateRangeSelectorModel>();

  public OnSubmit(form: NgForm): void {
    if (form.errors) {
      this.SubmitModel(undefined);
      return;
    }

    const date1: Date = form.value.datePicker1;
    const time1: TimePickerModel = form.value.timePicker1;
    const date2: Date = form.value.datePicker2;
    const time2: TimePickerModel = form.value.timePicker2;

    const model: IDateRangeSelectorModel = {
      start: time1.TransformDate(date1),
      end: time2.TransformDate(date2),
    };

    this.SubmitModel(model);
  }

  private SubmitModel(model: IDateRangeSelectorModel | undefined): void {
    this.DateRangeSelected.emit(model);
    this.onChange(model);
  }

  public IsDisabled: boolean = false;

  private onChange = (model: IDateRangeSelectorModel | undefined) => {};

  onTouched = () => {};

  writeValue(obj: IDateRangeSelectorModel): void {
    this.SubmitModel(obj);
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
}
