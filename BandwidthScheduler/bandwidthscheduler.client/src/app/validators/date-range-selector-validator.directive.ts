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
  selector: '[appDateRangeSelectorValidator]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: DateRangeSelectorValidatorDirective,
      multi: true,
    },
  ],
})
export class DateRangeSelectorValidatorDirective implements Validator {
  constructor() {}

  validate(control: AbstractControl<any, any>): ValidationErrors | null {
    const fg = <FormGroup>control;
    const controls = fg.controls;

    const errors: { [key: string]: any } = {};

    const date1: Date = controls['datePicker1']?.value;
    const time1: TimePickerModel = controls['timePicker1']?.value;

    const date2: Date = controls['datePicker2']?.value;
    const time2: TimePickerModel = controls['timePicker2']?.value;

    if (!date1) {
      errors['MissingDate1'] = true;
    }

    if (!time1) {
      errors['MissingTime1'] = true;
    }

    if (!date2) {
      errors['MissingDate2'] = true;
    }

    if (!time2) {
      errors['MissingTime2'] = true;
    }

    if (Object.keys(errors).length !== 0) return errors;

    const start = time1.TransformDate(date1);
    const end = time2.TransformDate(date2);

    if (start >= end) {
      errors['StartMustBeBefore'] = true;
    }

    return Object.keys(errors).length !== 0 ? errors : null;
  }
}
