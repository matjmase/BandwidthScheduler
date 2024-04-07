import { Directive } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { RegisterTemplateModel } from '../register/register.component';

@Directive({
  selector: '[appRegisterFormValidator]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: RegisterFormValidatorDirective,
      multi: true,
    },
  ],
})
export class RegisterFormValidatorDirective implements Validator {
  validate(control: AbstractControl<any, any>): ValidationErrors | null {
    const fg = <FormGroup>control;
    const model = <RegisterTemplateModel<string>>(<unknown>fg.value);

    const errors: any = {};
    if (model.email === '') {
      errors.EmptyEmail = true;
    }

    if (model.password === '') {
      errors.EmptyPassword = true;
    }

    if (model.password !== model.verify) {
      errors.MismatchPasswords = true;
    }

    if (!model.roles || Object.values(model.roles).every((e) => e !== true)) {
      errors.NoRoleSelected = true;
    }

    return Object.keys(errors).length === 0 ? null : errors;
  }
  registerOnValidatorChange?(fn: () => void): void {}
}
