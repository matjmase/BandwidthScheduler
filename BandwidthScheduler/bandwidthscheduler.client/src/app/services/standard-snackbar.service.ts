import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root',
})
export class StandardSnackbarService {
  constructor(private messageSnackBar: MatSnackBar) {}

  public OpenErrorMessage(message: string): void {
    this.messageSnackBar.open(message, 'Dismiss', {
      duration: 1000 * 3,
    });
  }

  public OpenConfirmationMessage(message: string): void {
    this.messageSnackBar.open(message, undefined, {
      duration: 1000 * 3,
    });
  }
}
