import { Directive } from '@angular/core';
import {
  AbstractControl,
  FormGroup,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { TimePickerModel } from '../models/TimePickerModel';

@Directive({
  selector: '[appDateTimeRangeSelectorValidator]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: DateTimeRangeSelectorValidatorDirective,
      multi: true,
    },
  ],
})
export class DateTimeRangeSelectorValidatorDirective implements Validator {
  constructor() {}

  validate(control: AbstractControl<any, any>): ValidationErrors | null {
    const fg = <FormGroup>control;
    const controls = fg.controls;

    let errors: { [key: string]: any } = {};

    const date: Date = controls['datePicker']?.value;
    const start: TimePickerModel = controls['startTimePicker']?.value;
    const end: TimePickerModel = controls['endTimePicker']?.value;

    if (!date) {
      errors['MissingDate'] = true;
    }

    if (!start) {
      errors['MissingStart'] = true;
    }

    if (!end) {
      errors['MissingEnd'] = true;
    }

    if (Object.keys(errors).length !== 0) return errors;

    const startTrans = start.TransformDate(date);
    const endTrans =
      DateTimeRangeSelectorValidatorDirective.EndDateSpecialHandling(end, date);

    if (startTrans >= endTrans) {
      errors['StartMustBeBefore'] = true;
    }

    return Object.keys(errors).length !== 0 ? errors : null;
  }

  public static EndDateSpecialHandling(time: TimePickerModel, day: Date): Date {
    const endTrans = time.TransformDate(day);

    if (time.hour === 0 && time.minute === 0 && time.second === 0) {
      endTrans.setDate(endTrans.getDate() + 1);
    }

    return endTrans;
  }
}
