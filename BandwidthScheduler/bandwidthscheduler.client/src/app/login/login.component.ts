import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BackendConnectService } from '../services/backend-connect.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  constructor(private backend: BackendConnectService, private router: Router) {}

  public Submit(form: NgForm): void {
    this.backend.Login.Login(<LoginTemplateModel<string>>form.value).subscribe({
      complete: () => this.router.navigate(['']),
      error: () => {},
    });
  }
}

export interface LoginTemplateModel<T> {
  email: T;
  password: T;
}
