import { Directive } from '@angular/core';
import {
  AbstractControl,
  FormGroup,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { RegisterTemplateModel } from '../register/register.component';
import { LoginTemplateModel } from '../login/login.component';

@Directive({
  selector: '[appLoginFormValidator]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: LoginFormValidatorDirective,
      multi: true,
    },
  ],
})
export class LoginFormValidatorDirective implements Validator {
  validate(control: AbstractControl<any, any>): ValidationErrors | null {
    const fg = <FormGroup>control;
    const model = <LoginTemplateModel<AbstractControl<any, any>>>(
      (<unknown>fg.controls)
    );

    const errors: any = {};
    if (model.email?.value === '') {
      errors.EmptyEmail = true;
    }

    if (model.password?.value === '') {
      errors.EmptyPassword = true;
    }

    return Object.keys(errors).length === 0 ? null : errors;
  }
  registerOnValidatorChange?(fn: () => void): void {}
}
