import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduleEditorComponent } from './schedule-editor.component';

describe('ScheduleEditorComponent', () => {
  let component: ScheduleEditorComponent;
  let fixture: ComponentFixture<ScheduleEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ScheduleEditorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ScheduleEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
