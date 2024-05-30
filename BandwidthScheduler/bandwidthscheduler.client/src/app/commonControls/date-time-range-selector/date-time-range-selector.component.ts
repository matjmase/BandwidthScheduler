import { Component, EventEmitter, Output, forwardRef } from '@angular/core';
import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  NgForm,
} from '@angular/forms';
import { DateTimeRangeSelectorModel } from './date-time-range-selector-model';
import { TimePickerModel } from '../../models/TimePickerModel';
import { DateTimeRangeSelectorValidatorDirective } from '../../validators/date-time-range-selector-validator.directive';

@Component({
  selector: 'app-date-time-range-selector',
  templateUrl: './date-time-range-selector.component.html',
  styleUrl: './date-time-range-selector.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateTimeRangeSelectorComponent),
      multi: true,
    },
  ],
})
export class DateTimeRangeSelectorComponent implements ControlValueAccessor {
  @Output()
  public DateTimeRangeSelected: EventEmitter<DateTimeRangeSelectorModel> =
    new EventEmitter<DateTimeRangeSelectorModel>();

  public OnSubmit(form: NgForm): void {
    const date: Date = form.value.datePicker;
    const startTime: TimePickerModel = form.value.startTimePicker;
    const endTime: TimePickerModel = form.value.endTimePicker;

    if (form.errors) {
      this.SubmitModel(undefined);
      return;
    }

    const model = {
      start: startTime.TransformDate(date),
      end: DateTimeRangeSelectorValidatorDirective.EndDateSpecialHandling(
        endTime,
        date
      ),
    };

    this.SubmitModel(model);
  }

  private SubmitModel(model: DateTimeRangeSelectorModel | undefined): void {
    this.DateTimeRangeSelected.emit(model);
    this.onChange(model);
  }

  public IsDisabled: boolean = false;

  // Function to call when the rating changes.
  private onChange = (model: DateTimeRangeSelectorModel | undefined) => {};

  // Function to call when the input is touched (when a star is clicked).
  onTouched = () => {};

  writeValue(obj: DateTimeRangeSelectorModel): void {
    this.SubmitModel(obj);
  }
  registerOnChange(
    fn: (model: DateTimeRangeSelectorModel | undefined) => {}
  ): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => {}): void {
    this.onTouched = fn;
  }
  setDisabledState?(isDisabled: boolean): void {
    this.IsDisabled = isDisabled;
  }
}
