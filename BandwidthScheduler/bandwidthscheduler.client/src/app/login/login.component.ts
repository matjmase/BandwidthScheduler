import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BackendConnectService } from '../services/backend-connect.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';

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
    private messageSnackBar: MatSnackBar
  ) {}

  public Submit(form: NgForm): void {
    this.waitingOnSubmit = true;

    this.backend.Login.Login(<LoginTemplateModel<string>>form.value).subscribe({
      complete: () => {
        this.router.navigate(['']);
        this.waitingOnSubmit = false;
      },
      error: (errorResp: HttpErrorResponse) => {
        this.openSnackBar(errorResp.error);
        this.waitingOnSubmit = false;
        form.resetForm();
      },
    });
  }

  private openSnackBar(message: string): void {
    this.messageSnackBar.open(message, 'Dismiss', {
      duration: 1000 * 3,
    });
  }
}

export interface LoginTemplateModel<T> {
  email: T;
  password: T;
}
