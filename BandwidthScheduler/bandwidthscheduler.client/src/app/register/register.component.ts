import { Component, OnInit, TemplateRef } from '@angular/core';
import { NgForm } from '@angular/forms';
import { LoginTemplateModel } from '../login/login.component';
import { BackendConnectService } from '../services/backend-connect.service';
import { RegisterCredentials } from '../models/IRegisterCredentials';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { StandardSnackbarService } from '../services/standard-snackbar.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent implements OnInit {
  roles: string[] | undefined;
  waitingOnSubmit: boolean = false;

  constructor(
    private backend: BackendConnectService,
    private router: Router,
    private messageSnackBar: StandardSnackbarService
  ) {}

  ngOnInit(): void {
    this.backend.Login.GetAllRoles().subscribe({
      next: (val) => (this.roles = val),
    });
  }

  public Submit(form: NgForm): void {
    this.waitingOnSubmit = true;
    const credentials = new RegisterCredentials(form.value);

    this.backend.Login.Register(credentials).subscribe({
      complete: () => {
        this.router.navigate(['']);
        this.waitingOnSubmit = false;
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.waitingOnSubmit = false;
        form.resetForm();
      },
    });
  }
}

export interface RegisterTemplateModel<T> extends LoginTemplateModel<T> {
  verify: T;
  type: T;
  roles: Object;
}
