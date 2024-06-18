import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BackendConnectService } from '../services/backend-connect.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { StandardSnackbarService } from '../services/standard-snackbar.service';
import { NotificationUpdateService } from '../services/notification-update.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  waitingOnSubmit: boolean = false;

  constructor(
    private backend: BackendConnectService,
    private router: Router,
    private messageSnackBar: StandardSnackbarService,
    private notification: NotificationUpdateService
  ) {}

  public Submit(form: NgForm): void {
    this.waitingOnSubmit = true;

    this.backend.Login.Login(<LoginTemplateModel<string>>form.value).subscribe({
      complete: () => {
        this.router.navigate(['']);
        this.waitingOnSubmit = false;
        this.notification.OnChange.next();
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.waitingOnSubmit = false;
        form.resetForm();
      },
    });
  }
}

export interface LoginTemplateModel<T> {
  email: T;
  password: T;
}
